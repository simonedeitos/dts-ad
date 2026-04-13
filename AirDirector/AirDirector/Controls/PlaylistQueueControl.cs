using AirDirector.Forms;
using AirDirector.Models;
using AirDirector.Services.Database;
using AirDirector.Themes;
using CsvHelper.Expressions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using AirDirector.Services.Localization;
using System.Windows.Forms;
using AirDirector.Themes;
using Microsoft.Win32;

namespace AirDirector.Controls
{
	public partial class PlaylistQueueControl : UserControl
	{
		private List<PlaylistQueueItem> _items;
		private int _currentPlayingIndex = -1;
		private VScrollBar _scrollBar;
		private int _scrollOffset = 0;
		private const int ITEM_HEIGHT = 80;
		private const int ITEM_MARGIN = 3;
		private const int CORNER_RADIUS = 8;
		private const int SIDE_BAR_WIDTH = 5;
		private const int BUTTON_WIDTH = 36;
		private const int CONTENT_PADDING = 10;
		private const int NUM_PANEL_WIDTH = 50;
		private const int ICON_PANEL_WIDTH = 42;

		private int _dragSourceIndex = -1;
		private bool _isDragging = false;
		private Point _dragStartPoint;
		private int _hoverButtonIndex = -1;
		private string _hoverButtonType = null; // "preview" o "delete" o null

		private bool _isPlayerStopped = true;
		private ToolTip _buttonToolTip;

		private System.Windows.Forms.Timer _queueMonitorTimer;
		private System.Windows.Forms.Timer _scheduleMonitorTimer;
		private DateTime _nextDayScheduleLoadTime;

		private bool _isInitialStartup = true;
		private string _currentClockName = "";
		private bool _isGeneratingClock = false;
		private List<PlaylistQueueItem> _generatingBatch = null;
		private readonly object _generatingBatchLock = new object();

		private PlaylistQueueItem _lastPlayedForReport = null;

		private Dictionary<string, DateTime> _queueSnapshotAtCreation = new Dictionary<string, DateTime>();

		private HashSet<string> _executedSchedules = new HashSet<string>();
        private string _pendingScheduleVideoBufferPath = "";
		private string _lastArtistFirstLetter = "";

		// ✅ FIX CRITICO: Aggiungi tracking reload come OverviewControl
		private List<AirDirectorPlaylistItem> _cachedAdvItems = new List<AirDirectorPlaylistItem>();
		private DateTime _advCacheDate = DateTime.MinValue;
		private DateTime _lastAdvReloadTime = DateTime.MinValue; // ✅ MANCAVA QUESTO!

		private Services.Core.DailyLogger _dailyLogger; 

		public event EventHandler<int> QueueReady;
		public event EventHandler<int> QueueCountChanged;
		public event EventHandler      ItemsAdded;
		public event EventHandler<string> PreviewRequested;
		public event EventHandler<string> ClockChanged;
		public event EventHandler ReportUpdated;

		public PlaylistQueueControl()
		{
			InitializeComponent();
			_items = new List<PlaylistQueueItem>();

			this.DoubleBuffered = true;
			this.BackColor = Color.Black;

			_buttonToolTip = new ToolTip
			{
				InitialDelay = 400,
				ReshowDelay = 200,
				AutoPopDelay = 3000
			};

			this.AllowDrop = true;
			this.DragEnter += PlaylistQueueControl_DragEnter;
			this.DragOver += PlaylistQueueControl_DragOver;
			this.DragDrop += PlaylistQueueControl_DragDrop;

			this.MouseDown += PlaylistQueueControl_MouseDown;
			this.MouseMove += PlaylistQueueControl_MouseMove;
			this.MouseUp += PlaylistQueueControl_MouseUp;

			_scrollBar = new VScrollBar
			{
				Dock = DockStyle.Right,
				Width = 20
			};
			_scrollBar.Scroll += ScrollBar_Scroll;
			this.Controls.Add(_scrollBar);

			this.Paint += PlaylistQueueControl_Paint;
			this.MouseWheel += PlaylistQueueControl_MouseWheel;
			this.Resize += (s, e) => UpdateScrollBar();

			_queueMonitorTimer = new System.Windows.Forms.Timer { Interval = 15000 };
			_queueMonitorTimer.Tick += QueueMonitorTimer_Tick;
			_queueMonitorTimer.Start();

			_scheduleMonitorTimer = new System.Windows.Forms.Timer { Interval = 30000 };
			_scheduleMonitorTimer.Tick += ScheduleMonitorTimer_Tick;
			_scheduleMonitorTimer.Start();

			CalculateNextDayScheduleLoadTime();
			LoadAdvCache();
			try { _dailyLogger = new Services.Core.DailyLogger("PlsQueue"); } catch { }

			Log($"[PlaylistADV] ✅ Costruttore completato - Cache caricata all'avvio");
		}

        private void PlaylistQueueControl_DragEnter(object sender, DragEventArgs e)
        {
            // ? Supporta sia drag interno che esterno
            if (e.Data.GetDataPresent("MusicEntryList") ||
                e.Data.GetDataPresent("ClipEntryList") ||
                e.Data.GetDataPresent(typeof(DragDropData)) ||
                _isDragging) // ? DRAG INTERNO
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void PlaylistQueueControl_DragOver(object sender, DragEventArgs e)
        {
            // ? Priorità a drag interno
            if (_isDragging && _dragSourceIndex >= 0)
            {
                e.Effect = DragDropEffects.Move;
                return;
            }

            // Drag esterno
            if (e.Data.GetDataPresent("MusicEntryList") ||
                e.Data.GetDataPresent("ClipEntryList") ||
                e.Data.GetDataPresent(typeof(DragDropData)))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void PlaylistQueueControl_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                Point clientPoint = this.PointToClient(new Point(e.X, e.Y));
                int targetIndex = GetItemIndexAtPoint(clientPoint);

                Log($"");
                Log($"[DragDrop] ========================================");
                Log($"[DragDrop] ?  Drop rilevato");
                Log($"[DragDrop] Target index: {targetIndex}");
                Log($"[DragDrop] Source index: {_dragSourceIndex}");
                Log($"[DragDrop] IsDragging: {_isDragging}");

                if (targetIndex < 0)
                    targetIndex = _items.Count;

                // ============================================
                // FIX CRITICO:  DRAG INTERNO
                // ============================================
                if (_isDragging && _dragSourceIndex >= 0)
                {
                    Log($"[DragDrop] ??  DRAG INTERNO RILEVATO");

                    // Protezioni
                    if (_dragSourceIndex == 0 && _currentPlayingIndex == 0)
                    {
                        Log("[DragDrop] ?? Blocco:  item in riproduzione (source)");
                        _dragSourceIndex = -1;
                        _isDragging = false;
                        return;
                    }

                    if (targetIndex == 0 && _currentPlayingIndex == 0)
                    {
                        Log("[DragDrop] ?? Blocco: drop su item in riproduzione");
                        _dragSourceIndex = -1;
                        _isDragging = false;
                        return;
                    }

                    // Stesso indice = nessun cambio
                    if (targetIndex == _dragSourceIndex)
                    {
                        Log("[DragDrop] ??  Stesso indice, nessuna azione");
                        _dragSourceIndex = -1;
                        _isDragging = false;
                        return;
                    }

                    // ESEGUI SPOSTAMENTO
                    Log($"[DragDrop] ??  Spostamento: {_dragSourceIndex} ?  {targetIndex}");

                    var item = _items[_dragSourceIndex];
                    Log($"[DragDrop] Item:  '{item.Title}'");

                    // Rimuovi dalla posizione originale
                    _items.RemoveAt(_dragSourceIndex);

                    // Aggiusta target se necessario
                    int finalTargetIndex = targetIndex;
                    if (_dragSourceIndex < targetIndex)
                    {
                        finalTargetIndex--;
                    }

                    // Inserisci nella nuova posizione
                    _items.Insert(finalTargetIndex, item);

                    Log($"[DragDrop] ?  Inserito a index: {finalTargetIndex}");

                    // Aggiorna indice corrente in riproduzione
                    if (_dragSourceIndex < _currentPlayingIndex && finalTargetIndex >= _currentPlayingIndex)
                    {
                        _currentPlayingIndex--;
                        Log($"[DragDrop] Current playing index aggiustato: {_currentPlayingIndex}");
                    }
                    else if (_dragSourceIndex > _currentPlayingIndex && finalTargetIndex <= _currentPlayingIndex)
                    {
                        _currentPlayingIndex++;
                        Log($"[DragDrop] Current playing index aggiustato: {_currentPlayingIndex}");
                    }
                    else if (_dragSourceIndex == _currentPlayingIndex)
                    {
                        _currentPlayingIndex = finalTargetIndex;
                        Log($"[DragDrop] Current playing spostato a: {_currentPlayingIndex}");
                    }

                    // Ricalcola tempi
                    RecalculateScheduledTimes();

                    // Refresh UI
                    UpdateScrollBar();
                    this.Invalidate();
                    NotifyQueueCountChanged();

                    Log($"[DragDrop] ? Spostamento completato con successo");
                    Log($"[DragDrop] ========================================");
                    Log($"");

                    // Reset stato drag
                    _dragSourceIndex = -1;
                    _isDragging = false;
                    return;
                }

                // ============================================
                // DRAG ESTERNO (da MusicArchive, ClipsArchive)
                // ============================================
                Log($"[DragDrop] ?? Verifica drag esterno...");

                if (e.Data.GetDataPresent("MusicEntryList"))
                {
                    var musicList = e.Data.GetData("MusicEntryList") as List<MusicEntry>;
                    if (musicList != null && musicList.Count > 0)
                    {
                        Log($"[DragDrop] ??  Drop {musicList.Count} music entries a index {targetIndex}");

                        foreach (var musicEntry in musicList)
                        {
                            var queueItem = CreateMusicQueueItem(musicEntry);
                            InsertItem(targetIndex, queueItem);
                            targetIndex++;
                        }

                        Log($"[DragDrop] ?  Music entries inserite");
                        return;
                    }
                }
                else if (e.Data.GetDataPresent("ClipEntryList"))
                {
                    var clipsList = e.Data.GetData("ClipEntryList") as List<ClipEntry>;
                    if (clipsList != null && clipsList.Count > 0)
                    {
                        Log($"[DragDrop] ?? Drop {clipsList.Count} clip entries a index {targetIndex}");

                        foreach (var clipEntry in clipsList)
                        {
                            var queueItem = CreateClipQueueItem(clipEntry);
                            InsertItem(targetIndex, queueItem);
                            targetIndex++;
                        }

                        Log($"[DragDrop] ? Clip entries inserite");
                        return;
                    }
                }
                else if (e.Data.GetDataPresent(typeof(DragDropData)))
                {
                    DragDropData dragData = e.Data.GetData(typeof(DragDropData)) as DragDropData;
                    if (dragData != null)
                    {
                        Log($"[DragDrop] ?? Drop DragDropData:  {dragData.EntryType}");

                        PlaylistQueueItem newItem = null;

                        if (dragData.EntryType == "Music" && dragData.EntryData is MusicEntry)
                            newItem = CreateMusicQueueItem(dragData.EntryData as MusicEntry);
                        else if (dragData.EntryType == "Clip" && dragData.EntryData is ClipEntry)
                            newItem = CreateClipQueueItem(dragData.EntryData as ClipEntry);

                        if (newItem != null)
                        {
                            InsertItem(targetIndex, newItem);
                            Log($"[DragDrop] ? DragDropData inserito");
                        }

                        return;
                    }
                }

                Log($"[DragDrop] ??  Nessun formato riconosciuto");
                Log($"[DragDrop] ========================================");
                Log($"");
            }
            catch (Exception ex)
            {
                Log($"");
                Log($"[DragDrop] ??  ERRORE CRITICO");
                Log($"[DragDrop] Messaggio: {ex.Message}");
                Log($"[DragDrop] StackTrace:");
                Log(ex.StackTrace);
                Log($"[DragDrop] ========================================");
                Log($"");

                MessageBox.Show(string.Format(LanguageManager.GetString("Queue.DropError", "Errore drop: {0}"), ex.Message), LanguageManager.GetString("Common.Error", "Errore"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Sempre reset stato drag
                _dragSourceIndex = -1;
                _isDragging = false;
            }
        }
        

        private void QueueMonitorTimer_Tick(object sender, EventArgs e)
		{
			try
			{
				int totalCount = _items.Count;

				if (totalCount <= 3)
				{
					_isInitialStartup = false;
					FillQueueFromLastClock();
				}
			}
			catch (Exception ex)
			{
				Log($"[QueueMonitor] Errore: {ex.Message}");
			}
		}

		// Timer a 1 minuto: controlla se nei prossimi 60 secondi ci sono schedulazioni,
		// le esegue immediatamente o tramite timer interno con il giusto ritardo.
		private void ScheduleMonitorTimer_Tick(object sender, EventArgs e)
		{
			try
			{
				DateTime now = DateTime.Now;

				bool shouldReload = false;
				string reloadReason = "";

				if (_cachedAdvItems.Count == 0)
				{
					shouldReload = true;
					reloadReason = "Cache vuota";
				}
				else if (_advCacheDate != now.Date)
				{
					shouldReload = true;
					reloadReason = "Cambio data";
				}
				else if (now.Hour == 23 && now.Minute == 59)
				{
					shouldReload = true;
					reloadReason = "23:59 - Precarica domani";
				}
				else if (now.Hour == 0 && now.Minute == 0)
				{
					shouldReload = true;
					reloadReason = "00:00 - Fallback mezzanotte";
				}
				else if (now.Minute == 0 && now.Hour >= 1)
				{
					shouldReload = true;
					reloadReason = $"{now.Hour:D2}:00 - Verifica oraria";
				}

				bool isMidnightReload = false;

				if (shouldReload)
				{
					isMidnightReload = (reloadReason == "Cambio data" || reloadReason == "00:00 - Fallback mezzanotte");
					Log($"[PlaylistADV] 🔄 Ricarica palinsesto: {reloadReason}");
					LoadAdvCache();
				}

				if (now >= _nextDayScheduleLoadTime && now < _nextDayScheduleLoadTime.AddMinutes(2))
				{
					LoadNextDaySchedules();
					CalculateNextDayScheduleLoadTime();
				}

				if (isMidnightReload)
				{
					CatchUpMissedSchedules(now);
					CatchUpMissedADVSchedules(now);
				}

				LookAheadAndExecuteSchedules(now);
				LookAheadAndExecuteADVSchedules(now);
			}
			catch { }
		}

		// Cerca schedulazioni nella finestra [now, now+60s] e le attiva subito o con timer interno.
		private void LookAheadAndExecuteSchedules(DateTime now)
		{
			try
			{
				int currentDayOfWeek = (int)now.DayOfWeek;
				var schedules = DbcManager.LoadFromCsv<ScheduleEntry>("Schedules.dbc");
				var activeSchedules = schedules
					.Where(s => s.IsEnabled == 1 && IsDayEnabled(s, currentDayOfWeek))
					.ToList();

				if (activeSchedules.Count == 0) return;

				TimeSpan windowStart = now.TimeOfDay;
				TimeSpan windowEnd = now.TimeOfDay.Add(TimeSpan.FromSeconds(60));

				foreach (var schedule in activeSchedules)
				{
					var times = schedule.Times.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

					foreach (var timeStr in times)
					{
						if (!TimeSpan.TryParse(timeStr, out TimeSpan scheduleTime)) continue;

						string scheduleKey = $"{schedule.Type}_{schedule.Name}_{timeStr}_{now:yyyy-MM-dd}";
						if (_executedSchedules.Contains(scheduleKey)) continue;

						bool inWindow = scheduleTime >= windowStart && scheduleTime < windowEnd;
						if (!inWindow) continue;

						double delayMs = (scheduleTime - now.TimeOfDay).TotalMilliseconds;

						// Segna subito come pianificata per evitare doppie attivazioni al minuto successivo
						_executedSchedules.Add(scheduleKey);

						if (delayMs <= 500)
						{
							Log($"[Schedules] ▶ Esecuzione immediata: {schedule.Name} @ {timeStr}");
							_isInitialStartup = false;
							_pendingScheduleVideoBufferPath = schedule.VideoBufferPath ?? "";
							ExecuteSchedule(schedule);
							_pendingScheduleVideoBufferPath = "";
						}
						else
						{
							int intervalMs = (int)Math.Ceiling(delayMs);
							Log($"[Schedules] ⏳ Schedulazione in attesa: {schedule.Name} @ {timeStr} (tra {intervalMs / 1000}s)");

							var capturedSchedule = schedule;
							var delayedTimer = new System.Windows.Forms.Timer { Interval = intervalMs };
							delayedTimer.Tick += (s, ev) =>
							{
								delayedTimer.Stop();
								delayedTimer.Dispose();
								Log($"[Schedules] ▶ Esecuzione ritardata: {capturedSchedule.Name} @ {timeStr}");
								_isInitialStartup = false;
								_pendingScheduleVideoBufferPath = capturedSchedule.VideoBufferPath ?? "";
								ExecuteSchedule(capturedSchedule);
								_pendingScheduleVideoBufferPath = "";
							};
							delayedTimer.Start();
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log($"[Schedules] ❌ ERRORE LookAhead: {ex.Message}");
			}
		}

		// Cerca schedulazioni ADV nella finestra [now, now+90s] e le attiva subito o con timer interno.
		private void LookAheadAndExecuteADVSchedules(DateTime now)
		{
			try
			{
				if (_cachedAdvItems.Count == 0 || _advCacheDate != now.Date)
					LoadAdvCache();

				if (_cachedAdvItems.Count == 0)
				{
					Log($"[PlaylistADV] ⚠️ LookAhead: nessun item in cache.");
					return;
				}

				var todaySlots = _cachedAdvItems
					.Where(a => a.Date.Date == now.Date && a.IsActive)
					.GroupBy(a => a.SlotTime)
					.Select(g => new { SlotTime = g.Key, Items = g.OrderBy(x => x.SequenceOrder).ToList() })
					.ToList();

				Log($"[PlaylistADV] LookAhead {now:HH:mm:ss}: slot oggi={todaySlots.Count}, item totali={_cachedAdvItems.Count(a => a.Date.Date == now.Date)}");

				TimeSpan windowStart = now.TimeOfDay;
				TimeSpan windowEnd = now.TimeOfDay.Add(TimeSpan.FromSeconds(90));

				foreach (var slot in todaySlots)
				{
					if (!TimeSpan.TryParse(slot.SlotTime, out TimeSpan slotTime))
					{
						Log($"[PlaylistADV] ⚠️ SlotTime non parsabile: '{slot.SlotTime}'");
						continue;
					}

					bool inWindow = slotTime >= windowStart && slotTime < windowEnd;
					if (!inWindow) continue;

					Log($"[PlaylistADV]   Slot {slot.SlotTime}: in finestra ({windowStart:hh\\:mm\\:ss}–{windowEnd:hh\\:mm\\:ss}), item={slot.Items.Count}");

					string advKey = $"ADV_{slot.SlotTime}_{now:yyyy-MM-dd}";
					if (_executedSchedules.Contains(advKey))
					{
						Log($"[PlaylistADV]   Slot {slot.SlotTime}: già eseguito, skip.");
						continue;
					}

					double delayMs = (slotTime - now.TimeOfDay).TotalMilliseconds;

					// Segna subito come pianificata per evitare doppie attivazioni
					_executedSchedules.Add(advKey);

					var capturedSlot = slot;

					if (delayMs <= 500)
					{
						Log($"[PlaylistADV] ▶ Esecuzione immediata slot {slot.SlotTime} (ritardo={delayMs:F0}ms)");
						ExecuteADVSlot(now, capturedSlot.SlotTime, capturedSlot.Items);
					}
					else
					{
						int intervalMs = (int)Math.Ceiling(delayMs);
						Log($"[PlaylistADV] ⏳ ADV in attesa: {slot.SlotTime} (tra {intervalMs / 1000}s)");

						var delayedTimer = new System.Windows.Forms.Timer { Interval = intervalMs };
						delayedTimer.Tick += (s, ev) =>
						{
							delayedTimer.Stop();
							delayedTimer.Dispose();
							ExecuteADVSlot(now, capturedSlot.SlotTime, capturedSlot.Items);
						};
						delayedTimer.Start();
					}
				}
			}
			catch (Exception ex)
			{
				Log($"[PlaylistADV] ❌ Errore LookAhead ADV: {ex.Message}");
			}
		}

		// Recupera schedulazioni mancate tra 00:00:00 e now (solo entro i primi 2 minuti dalla mezzanotte).
		private void CatchUpMissedSchedules(DateTime now)
		{
			try
			{
				TimeSpan catchUpLimit = TimeSpan.FromMinutes(2);
				if (now.TimeOfDay > catchUpLimit) return;

				int currentDayOfWeek = (int)now.DayOfWeek;
				var schedules = DbcManager.LoadFromCsv<ScheduleEntry>("Schedules.dbc");
				var activeSchedules = schedules
					.Where(s => s.IsEnabled == 1 && IsDayEnabled(s, currentDayOfWeek))
					.ToList();

				if (activeSchedules.Count == 0) return;

				TimeSpan windowEnd = now.TimeOfDay;

				foreach (var schedule in activeSchedules)
				{
					var times = schedule.Times.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

					foreach (var timeStr in times)
					{
						if (!TimeSpan.TryParse(timeStr, out TimeSpan scheduleTime)) continue;
						if (scheduleTime > windowEnd) continue;

						string scheduleKey = $"{schedule.Type}_{schedule.Name}_{timeStr}_{now:yyyy-MM-dd}";
						if (_executedSchedules.Contains(scheduleKey)) continue;

						Log($"[Schedules] 🔄 Catch-up: {schedule.Name} @ {timeStr} (mancata durante reload)");
						_executedSchedules.Add(scheduleKey);
						_isInitialStartup = false;
						_pendingScheduleVideoBufferPath = schedule.VideoBufferPath ?? "";
						ExecuteSchedule(schedule);
						_pendingScheduleVideoBufferPath = "";
					}
				}
			}
			catch (Exception ex)
			{
				Log($"[Schedules] ❌ Errore CatchUp Schedules: {ex.Message}");
			}
		}

		// Recupera slot ADV mancati tra 00:00:00 e now (solo entro i primi 2 minuti dalla mezzanotte).
		private void CatchUpMissedADVSchedules(DateTime now)
		{
			try
			{
				TimeSpan catchUpLimit = TimeSpan.FromMinutes(2);
				if (now.TimeOfDay > catchUpLimit) return;

				if (_cachedAdvItems.Count == 0) return;

				var todaySlots = _cachedAdvItems
					.Where(a => a.Date.Date == now.Date && a.IsActive)
					.GroupBy(a => a.SlotTime)
					.Select(g => new { SlotTime = g.Key, Items = g.OrderBy(x => x.SequenceOrder).ToList() })
					.ToList();

				TimeSpan windowEnd = now.TimeOfDay;

				foreach (var slot in todaySlots)
				{
					if (!TimeSpan.TryParse(slot.SlotTime, out TimeSpan slotTime)) continue;
					if (slotTime > windowEnd) continue;

					string advKey = $"ADV_{slot.SlotTime}_{now:yyyy-MM-dd}";
					if (_executedSchedules.Contains(advKey)) continue;

					Log($"[PlaylistADV] 🔄 Catch-up ADV slot {slot.SlotTime} (mancato durante reload)");
					_executedSchedules.Add(advKey);
					ExecuteADVSlot(now, slot.SlotTime, slot.Items);
				}
			}
			catch (Exception ex)
			{
				Log($"[PlaylistADV] ❌ Errore CatchUp ADV: {ex.Message}");
			}
		}

		private void ExecuteADVSlot(DateTime now, string slotTime, System.Collections.Generic.List<AirDirectorPlaylistItem> items)
		{
			Log($"");
			Log($"╔════════════════════════════════════════════════════════════╗");
			Log($"║  PUBBLICITÀ ATTIVATA: {slotTime}                           ║");
			Log($"╚════════════════════════════════════════════════════════════╝");

			int insertPosition = GetCorrectScheduleInsertPosition();
			int addedCount = 0;

			foreach (var advFile in items)
			{
				var advSlot = CreateSingleADVSlot(advFile, slotTime);
				if (advSlot != null)
				{
					InsertItemBatch(insertPosition, advSlot);
					insertPosition++;
					addedCount++;
					Log($"[PlaylistADV]   → {advFile.FileType}: {Path.GetFileName(advFile.FilePath)} ({advFile.Duration}s)");
				}
			}

			if (addedCount > 0)
				FinalizeBatchModification();

			Log($"[PlaylistADV] ✅ Inseriti {addedCount} slot ADV");
			Log($"");
		}

		// ✅ FIX:  Metodo IDENTICO a OverviewControl
		private void CheckAndExecuteADVSchedules(DateTime now)
		{
			try
			{
				if (_cachedAdvItems.Count == 0 || _advCacheDate != now.Date)
				{
					LoadAdvCache();
				}

				if (_cachedAdvItems.Count == 0)
					return;

				// ✅ IDENTICO A OVERVIEWCONTROL
				var todaySlots = _cachedAdvItems
					.Where(a => a.Date.Date == now.Date && a.IsActive)
					.GroupBy(a => a.SlotTime)
					.Select(g => new { SlotTime = g.Key, Items = g.OrderBy(x => x.SequenceOrder).ToList() })
					.ToList();

				foreach (var slot in todaySlots)
				{
					if (TimeSpan.TryParse(slot.SlotTime, out TimeSpan slotTime))
					{
						TimeSpan currentTimeSpan = now.TimeOfDay;

						if (currentTimeSpan.Hours == slotTime.Hours &&
							currentTimeSpan.Minutes == slotTime.Minutes &&
							currentTimeSpan.Seconds == slotTime.Seconds)
						{
							string advKey = $"ADV_{slot.SlotTime}_{now: yyyy-MM-dd}";

							if (_executedSchedules.Contains(advKey))
								return;

							Log($"");
							Log($"╔════════════════════════════════════════════════════════════╗");
							Log($"║  PUBBLICITÀ ATTIVATA: {slot.SlotTime}                           ║");
							Log($"╚════════════════════════════════════════════════════════════╝");

							int insertPosition = GetCorrectScheduleInsertPosition();
							int addedCount = 0;

							foreach (var advFile in slot.Items)
							{
								var advSlot = CreateSingleADVSlot(advFile, slot.SlotTime);

								if (advSlot != null)
								{
									InsertItemBatch(insertPosition, advSlot);
									insertPosition++;
									addedCount++;
									Log($"[PlaylistADV]   → {advFile.FileType}:  {Path.GetFileName(advFile.FilePath)} ({advFile.Duration}s)");
								}
							}

							if (addedCount > 0)
								FinalizeBatchModification();

							_executedSchedules.Add(advKey);

							Log($"[PlaylistADV] ✅ Inseriti {addedCount} slot ADV");
							Log($"");

							return;
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log($"[PlaylistADV] ❌ Errore:  {ex.Message}");
				Log($"[PlaylistADV] StackTrace: {ex.StackTrace}");
			}
		}

		private PlaylistQueueItem CreateSingleADVSlot(AirDirectorPlaylistItem advItem, string slotTime)
		{
			try
			{
				if (!File.Exists(advItem.FilePath))
				{
					Log($"[PlaylistADV] ⚠️ File non trovato: {advItem.FilePath}");
					return null;
				}

				TimeSpan duration = GetAudioDuration(advItem.FilePath);

				string title = $"{advItem.FileType}";

				if (!string.IsNullOrEmpty(advItem.SpotTitle))
					title += $" - {advItem.SpotTitle}";
				else if (!string.IsNullOrEmpty(advItem.ClientName))
					title += $" - {advItem.ClientName}";

				var advSlot = new PlaylistQueueItem
				{
					Type = PlaylistItemType.ADV,
					ScheduledTime = CalculateScheduledPlayTime(),
					Artist = "",
					Title = title,
					Year = 0,
					Duration = duration,
					Intro = TimeSpan.Zero,
					FilePath = advItem.FilePath,
					MarkerIN = 0,
					MarkerINTRO = 0,
					MarkerMIX = 0,
					MarkerOUT = 0,
					IsScheduled = true,
					ADVSpotCount = (advItem.FileType == "SPOT" ? 1 : 0),
					ADVFileCount = 1
				};

				return advSlot;
			}
			catch (Exception ex)
			{
				Log($"[PlaylistADV] Errore creazione slot: {ex.Message}");
				return null;
			}
		}

		private void CalculateNextDayScheduleLoadTime()
		{
			DateTime today = DateTime.Now.Date;
			_nextDayScheduleLoadTime = today.AddHours(23).AddMinutes(55);

			if (DateTime.Now >= _nextDayScheduleLoadTime)
				_nextDayScheduleLoadTime = _nextDayScheduleLoadTime.AddDays(1);
		}

		private void LoadNextDaySchedules()
		{
			try
			{
				DateTime tomorrow = DateTime.Now.Date.AddDays(1);
				int tomorrowDayOfWeek = (int)tomorrow.DayOfWeek;

				var schedules = DbcManager.LoadFromCsv<ScheduleEntry>("Schedules.dbc");
				var tomorrowSchedules = schedules.Where(s => s.IsEnabled == 1 && IsDayEnabled(s, tomorrowDayOfWeek)).ToList();

				_executedSchedules.Clear();
			}
			catch { }
		}

		public void ReloadAdvSchedules()
		{
			LoadAdvCache();
			Log("[PlaylistADV] ✅ Cache ADV ricaricata manualmente");
		}

		// ✅ FIX:  LoadAdvCache IDENTICO a OverviewControl
		private void LoadAdvCache()
		{
			try
			{
				string databasePath = "";

				try
				{
					using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AirDirector"))
					{
						if (key != null)
							databasePath = key.GetValue("DatabasePath") as string;
					}
				}
				catch
				{
					databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database");
				}

				if (string.IsNullOrEmpty(databasePath))
					databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database");

				string advFilePath = Path.Combine(databasePath, "ADV_AirDirector.dbc");

				Log($"");
				Log($"[PlaylistADV] ━━━ CARICAMENTO CACHE ━━━");
				Log($"[PlaylistADV] Ora: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
				Log($"[PlaylistADV] DatabasePath: {databasePath}");
				Log($"[PlaylistADV] File: {advFilePath}");
				Log($"[PlaylistADV] Esiste: {File.Exists(advFilePath)}");

				if (!File.Exists(advFilePath))
				{
					Log($"[PlaylistADV] ❌ FILE NON TROVATO!");
					_cachedAdvItems.Clear();
					_advCacheDate = DateTime.MinValue;
					_lastAdvReloadTime = DateTime.Now; // ✅ AGGIORNATO
					return;
				}

				var lines = File.ReadAllLines(advFilePath);
				Log($"[PlaylistADV] Righe file: {lines.Length}");

				_cachedAdvItems.Clear();

				var italianCulture = new System.Globalization.CultureInfo("it-IT");

				for (int i = 1; i < lines.Length; i++)
				{
					try
					{
						string line = lines[i].Trim().TrimStart('\uFEFF');
						if (string.IsNullOrEmpty(line)) continue;
						if (line.StartsWith("\"") && line.EndsWith("\"") && line.Length >= 2)
							line = line.Substring(1, line.Length - 2);
						var parts = line.Split(new[] { "\";\""  }, StringSplitOptions.None);
						if (parts.Length < 9)
						{
							Log($"[PlaylistADV] ⚠️ Riga {i} saltata: solo {parts.Length} campi (attesi 9). Contenuto: '{lines[i].Trim()}'");
							continue;
						}

						// Pulisci eventuali virgolette residue su ogni campo
						for (int p = 0; p < parts.Length; p++)
							parts[p] = parts[p].Trim('"').Trim();

						var item = new AirDirectorPlaylistItem
						{
							ID = int.Parse(parts[0]),
							Date = DateTime.Parse(parts[1], italianCulture),
							SlotTime = parts[2],
							SequenceOrder = int.Parse(parts[3]),
							FileType = parts[4],
							FilePath = parts[5],
							Duration = int.Parse(parts[6]),
							ClientName = parts[7],
							SpotTitle = parts[8],
							CampaignName = "",
							CategoryName = "",
							IsActive = true
						};

						_cachedAdvItems.Add(item);
					}
					catch (Exception ex)
					{
						Log($"[PlaylistADV] ⚠️ Riga {i} saltata: {ex.Message} — Contenuto: '{lines[i].Trim()}'");
					}
				}

				_advCacheDate = DateTime.Now.Date;
				_lastAdvReloadTime = DateTime.Now; // ✅ AGGIORNATO

				DateTime oggi = DateTime.Now.Date;
				DateTime domani = oggi.AddDays(1);

				int itemOggi = _cachedAdvItems.Count(a => a.Date.Date == oggi);
				int itemDomani = _cachedAdvItems.Count(a => a.Date.Date == domani);

				Log($"[PlaylistADV] ✅ Totale caricati: {_cachedAdvItems.Count}");
				Log($"[PlaylistADV] 📅 Item oggi ({oggi: dd/MM/yyyy}): {itemOggi}");
				Log($"[PlaylistADV] 📅 Item domani ({domani:dd/MM/yyyy}): {itemDomani}");
				Log($"[PlaylistADV] Data cache impostata:  {_advCacheDate:yyyy-MM-dd}");

				var slotsOggi = _cachedAdvItems
					.Where(a => a.Date.Date == oggi)
					.Select(a => a.SlotTime)
					.Distinct()
					.OrderBy(s => s)
					.ToList();

				Log($"[PlaylistADV] 🕐 Slot programmati oggi: {string.Join(", ", slotsOggi)}");
				Log($"[PlaylistADV] ━━━━━━━━━━━━━━━━━━━━━━━━");
				Log($"");
			}
			catch (Exception ex)
			{
				Log($"[PlaylistADV] ❌ ERRORE CRITICO: {ex.Message}");
				Log($"[PlaylistADV] StackTrace: {ex.StackTrace}");
				_cachedAdvItems.Clear();
				_advCacheDate = DateTime.MinValue;
				_lastAdvReloadTime = DateTime.Now; // ✅ AGGIORNATO
			}
		}

        private void CheckAndExecuteSchedules(DateTime now)
        {
            try
            {
                int currentDayOfWeek = (int)now.DayOfWeek;

                Log($"");
                Log($"[Schedules] ============================================");
                Log($"[Schedules] ? Controllo ore {now:HH:mm:ss}");
                Log($"[Schedules] ?? Giorno:   {now.DayOfWeek} (numero {currentDayOfWeek})");

                var schedules = DbcManager.LoadFromCsv<ScheduleEntry>("Schedules.dbc");

                Log($"[Schedules] ?? Totale schedules nel file: {schedules.Count}");

                var enabledSchedules = schedules.Where(s => s.IsEnabled == 1).ToList();
                Log($"[Schedules] ? Schedules abilitate:   {enabledSchedules.Count}");

                var activeSchedules = enabledSchedules.Where(s => IsDayEnabled(s, currentDayOfWeek)).ToList();
                Log($"[Schedules] ?? Schedules attive oggi:  {activeSchedules.Count}");

                if (activeSchedules.Count == 0)
                {
                    Log($"[Schedules] ?? Nessuna schedule attiva per oggi");
                    Log($"[Schedules] ============================================");
                    Log($"");
                    return;
                }

                // Log delle schedule attive
                foreach (var schedule in activeSchedules)
                {
                    Log($"[Schedules]   - {schedule.Name} ({schedule.Type}) @ {schedule.Times}");
                }

                foreach (var schedule in activeSchedules)
                {
                    var times = schedule.Times.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var timeStr in times)
                    {
                        if (TimeSpan.TryParse(timeStr, out TimeSpan scheduleTime))
                        {
                            string scheduleKey = $"{schedule.Type}_{schedule.Name}_{timeStr}_{now:  yyyy-MM-dd}";

                            if (_executedSchedules.Contains(scheduleKey))
                            {
                                continue; // Già eseguita oggi
                            }

                            TimeSpan currentTimeSpan = now.TimeOfDay;

                            // Match preciso al secondo
                            if (currentTimeSpan.Hours == scheduleTime.Hours &&
                                currentTimeSpan.Minutes == scheduleTime.Minutes &&
                                currentTimeSpan.Seconds == scheduleTime.Seconds)
                            {
                                Log($"");
                                Log($"+============================================================+");
                                Log($"|  ?? SCHEDULAZIONE ATTIVATA:   {schedule.Name,-30} |");
                                Log($"+============================================================+");
                                Log($"[Schedule] ?? Nome:   {schedule.Name}");
                                Log($"[Schedule] ?? Tipo:  {schedule.Type}");
                                Log($"[Schedule] ? Orario:   {timeStr}");
                                Log($"[Schedule] ?? Key:  {scheduleKey}");
                                Log($"");

                                _isInitialStartup = false;
                                _pendingScheduleVideoBufferPath = schedule.VideoBufferPath ?? "";
                                ExecuteSchedule(schedule);
                                _pendingScheduleVideoBufferPath = "";

                                _executedSchedules.Add(scheduleKey);

                                Log($"[Schedule] ? Schedulazione completata");
                                Log($"");

                                return; // Esegui solo una schedule per tick
                            }
                        }
                        else
                        {
                            Log($"[Schedules] ?? Formato ora non valido:   {timeStr}");
                        }
                    }
                }

                Log($"[Schedules] ?? Nessuna schedule da eseguire adesso");
                Log($"[Schedules] ============================================");
                Log($"");
            }
            catch (Exception ex)
            {
                Log($"");
                Log($"[Schedules] ??? ERRORE CRITICO ???");
                Log($"[Schedules] Messaggio: {ex.Message}");
                Log($"[Schedules] StackTrace:");
                Log(ex.StackTrace);
                Log($"[Schedules] =======================================");
                Log($"");
            }
        }

        private void ExecuteSchedule(ScheduleEntry schedule)
        {
            try
            {
                Log($"[ExecuteSchedule] ?? Esecuzione:   {schedule.Name}");
                Log($"[ExecuteSchedule] Tipo: {schedule.Type}");

                switch (schedule.Type)
                {
                    case "PlayClock":
                        if (!string.IsNullOrEmpty(schedule.ClockName))
                        {
                            Log($"[ExecuteSchedule] ? Clock: {schedule.ClockName}");
                            ClearNonPlayingItems();
                            GeneratePlaylistFromClock(schedule.ClockName, 1000);
                        }
                        else
                        {
                            Log($"[ExecuteSchedule] ?? ClockName vuoto!  ");
                        }
                        break;

                    case "PlayAudio":
                        if (!string.IsNullOrEmpty(schedule.AudioFilePath))
                        {
                            Log($"[ExecuteSchedule] ?? Audio:   {schedule.AudioFilePath}");
                            InsertScheduledAudioFile(schedule.Name, schedule.AudioFilePath);
                        }
                        else
                        {
                            Log($"[ExecuteSchedule] ?? AudioFilePath vuoto!  ");
                        }
                        break;

                    case "PlayMiniPLS":
                        if (schedule.MiniPLSID > 0)
                        {
                            Log($"[ExecuteSchedule] ?? MiniPLS ID:   {schedule.MiniPLSID}");
                            InsertScheduledMiniPlaylist(schedule.MiniPLSID);
                        }
                        else
                        {
                            Log($"[ExecuteSchedule] ?? MiniPLSID non valido!  ");
                        }
                        break;

                    case "PlayPlaylist":
                        if (!string.IsNullOrEmpty(schedule.AudioFilePath))
                        {
                            Log($"[ExecuteSchedule] 📋 Playlist: {schedule.AudioFilePath}");
                            try
                            {
                                var playlist = AirPlaylist.Load(schedule.AudioFilePath);
                                if (playlist != null)
                                {
                                    LoadPlaylistOnAir(playlist, true);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log($"[ExecuteSchedule] ⚠️ Errore caricamento playlist: {ex.Message}");
                            }
                        }
                        else
                        {
                            Log($"[ExecuteSchedule] ⚠️ AudioFilePath vuoto per PlayPlaylist!");
                        }
                        break;

                    case "TimeSignal":
                        Log($"[ExecuteSchedule] ? Segnale Orario");
                        InsertTimeSignal(schedule.Name);
                        break;

                    case "URLStreaming":
                        if (!string.IsNullOrEmpty(schedule.ClockName))
                        {
                            Log($"[ExecuteSchedule] ?? Streaming:  {schedule.ClockName}");
                            InsertURLStreaming(schedule);
                        }
                        else
                        {
                            Log($"[ExecuteSchedule] ?? URL Streaming vuoto!  ");
                        }
                        break;

                    default:
                        Log($"[ExecuteSchedule] ?? Tipo sconosciuto: {schedule.Type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Log($"[ExecuteSchedule] ? ERRORE:   {ex.Message}");
                Log($"[ExecuteSchedule] StackTrace: {ex.StackTrace}");
            }
        }

        private void InsertTimeSignal(string scheduleName)
		{
			try
			{
				string timeSignalPath = ConfigurationControl.GetTimeSignalPath();

				if (string.IsNullOrEmpty(timeSignalPath) || !Directory.Exists(timeSignalPath))
				{
					Log("[TimeSignal] ⚠️ Cartella non configurata");
					return;
				}

				DateTime scheduledPlayTime = CalculateScheduledPlayTime();
				string fileName = scheduledPlayTime.ToString("HHmm");

				string[] possibleExtensions = { ".mp3", ".wav", ".flac", ".m4a", ".wma", ".aac", ".mp4" };
				string foundFile = null;

				foreach (var ext in possibleExtensions)
				{
					string testPath = Path.Combine(timeSignalPath, fileName + ext);
					if (File.Exists(testPath))
					{
						foundFile = testPath;
						break;
					}
				}

				if (foundFile == null)
				{
					Log($"[TimeSignal] ⚠️ File non trovato: {fileName}");
					return;
				}

				TimeSpan duration = GetAudioDuration(foundFile);

				var timeSignalItem = new PlaylistQueueItem
				{
					Type = PlaylistItemType.Other,
					ScheduledTime = scheduledPlayTime,
					Artist = "",
					Title = scheduleName,
					Year = 0,
					Duration = duration,
					Intro = TimeSpan.Zero,
					FilePath = foundFile,
					MarkerIN = 0,
					MarkerINTRO = 0,
					MarkerMIX = 0,
					MarkerOUT = 0,
					IsScheduled = true
				};

				int insertPosition = GetCorrectScheduleInsertPosition();
				ApplyScheduleVideoBuffer(timeSignalItem);
				InsertItem(insertPosition, timeSignalItem);

				Log($"[TimeSignal] ✅ Inserito:  {scheduleName}");
			}
			catch (Exception ex)
			{
				Log($"[TimeSignal] Errore: {ex.Message}");
			}
		}

		private DateTime CalculateScheduledPlayTime()
		{
			try
			{
				DateTime predictedTime = DateTime.Now;

				if (_items.Count > 0 && _currentPlayingIndex == 0)
				{
					var playingItem = _items[0];
					TimeSpan elapsed = DateTime.Now - playingItem.ActualStartTime;
					TimeSpan remaining = playingItem.Duration - elapsed;

					if (remaining.TotalSeconds > 0)
						predictedTime = DateTime.Now.Add(remaining);
				}

				if (predictedTime.Second >= 30)
					predictedTime = predictedTime.AddMinutes(1);

				predictedTime = new DateTime(predictedTime.Year, predictedTime.Month, predictedTime.Day,
											 predictedTime.Hour, predictedTime.Minute, 0);

				return predictedTime;
			}
			catch
			{
				return DateTime.Now;
			}
		}

		private void ApplyScheduleVideoBuffer(PlaylistQueueItem item)
		{
			if (item == null || string.IsNullOrEmpty(_pendingScheduleVideoBufferPath)) return;
			if (!string.IsNullOrEmpty(item.VideoFilePath)) return; // Don't override existing video association
			item.VideoFilePath = _pendingScheduleVideoBufferPath;
			item.VideoSource = "BufferVideo";
		}

		private int GetCorrectScheduleInsertPosition()
		{
			if (_items.Count == 0 || _currentPlayingIndex != 0)
				return 0;

			int insertPosition = 1;

			for (int i = 1; i < _items.Count; i++)
			{
				if (_items[i].IsScheduled)
					insertPosition = i + 1;
				else
					break;
			}

			return insertPosition;
		}

		private void InsertURLStreaming(ScheduleEntry schedule)
		{
			try
			{
				if (string.IsNullOrEmpty(schedule.ClockName))
					return;

				var parts = schedule.ClockName.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

				if (parts.Length < 1)
					return;

				string url = parts[0].Trim();
				TimeSpan duration = TimeSpan.FromMinutes(60);

				if (parts.Length >= 2 && TimeSpan.TryParse(parts[1].Trim(), out TimeSpan parsedDuration))
					duration = parsedDuration;

				DateTime scheduledPlayTime = CalculateScheduledPlayTime();

				var streamingItem = new PlaylistQueueItem
				{
					Type = PlaylistItemType.Other,
					ScheduledTime = scheduledPlayTime,
					Artist = "",
					Title = $"WebStreaming - {schedule.Name}",
					Year = 0,
					Duration = duration,
					Intro = TimeSpan.Zero,
					FilePath = url,
					MarkerIN = 0,
					MarkerINTRO = 0,
					MarkerMIX = 0,
					MarkerOUT = 0,
					IsScheduled = true
				};

				int insertPosition = GetCorrectScheduleInsertPosition();
				ApplyScheduleVideoBuffer(streamingItem);
				InsertItem(insertPosition, streamingItem);

				Log($"[URLStreaming] ✅ Inserito: {schedule.Name}");
			}
			catch (Exception ex)
			{
				Log($"[URLStreaming] Errore: {ex.Message}");
			}
		}

		private TimeSpan GetAudioDuration(string filePath)
		{
			try
			{
				using (var tagFile = TagLib.File.Create(filePath))
				{
					return tagFile.Properties.Duration;
				}
			}
			catch
			{
				return TimeSpan.FromSeconds(30);
			}
		}

		private void ClearNonPlayingItems()
		{
			const int protectionWindowMinutes = 10;

			if (_currentPlayingIndex == 0 && _items.Count > 0)
			{
				var playingItem = _items[0];

				// Collect scheduled/ADV items that should be preserved
				var preservedItems = new List<PlaylistQueueItem>();
				double accumulatedMs = playingItem.Duration.TotalMilliseconds;

				for (int i = 1; i < _items.Count; i++)
				{
					var item = _items[i];
					bool isProtected = item.IsScheduled || item.Type == PlaylistItemType.ADV;

					if (isProtected && accumulatedMs <= protectionWindowMinutes * 60 * 1000)
					{
						preservedItems.Add(item);
					}

					accumulatedMs += item.Duration.TotalMilliseconds;
				}

				_items.Clear();
				_items.Add(playingItem);
				_items.AddRange(preservedItems);
				_currentPlayingIndex = 0;

				if (preservedItems.Count > 0)
					Log($"[ClearNonPlayingItems] Preserved {preservedItems.Count} scheduled/ADV items within {protectionWindowMinutes} min window");
			}
			else
			{
				_items.Clear();
				_currentPlayingIndex = -1;
			}

			UpdateScrollBar();
			this.Invalidate();
			NotifyQueueCountChanged();
		}

		private void InsertScheduledAudioFile(string scheduleName, string filePath)
		{
			try
			{
				if (!File.Exists(filePath))
				{
					Log($"[InsertScheduledAudioFile] ⚠️ File non trovato: {filePath}");
					return;
				}

				var allMusic = DbcManager.LoadFromCsv<MusicEntry>("Music.dbc");
				var allClips = DbcManager.LoadFromCsv<ClipEntry>("Clips.dbc");

				// Case-insensitive comparison for Windows path robustness
				var musicEntry = allMusic.FirstOrDefault(m => string.Equals(m.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
				var clipEntry = allClips.FirstOrDefault(c => string.Equals(c.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

				PlaylistQueueItem scheduledItem = null;

				if (musicEntry != null)
					scheduledItem = CreateMusicQueueItem(musicEntry);
				else if (clipEntry != null)
					scheduledItem = CreateClipQueueItem(clipEntry);
				else
				{
					// File exists on disk but is not registered in Music.dbc or Clips.dbc.
					// Create a queue item directly from the file using tag metadata.
					Log($"[InsertScheduledAudioFile] ℹ️ File non in archivio, lettura diretta da file");

					TimeSpan duration = GetAudioDuration(filePath);
					string title = scheduleName;
					string artist = "";

					try
					{
						using (var tagFile = TagLib.File.Create(filePath))
						{
							if (!string.IsNullOrEmpty(tagFile.Tag.Title))
								title = tagFile.Tag.Title;
							if (tagFile.Tag.Performers?.Length > 0)
								artist = tagFile.Tag.Performers[0];
						}
					}
					catch (Exception tagEx)
					{
						Log($"[InsertScheduledAudioFile] ℹ️ Lettura tag non riuscita, usato nome schedulazione: {tagEx.Message}");
					}

					scheduledItem = new PlaylistQueueItem
					{
						Type = PlaylistItemType.Other,
						ScheduledTime = CalculateScheduledPlayTime(),
						Artist = artist,
						Title = title,
						Year = 0,
						Duration = duration,
						Intro = TimeSpan.Zero,
						FilePath = filePath,
						MarkerIN = 0,
						MarkerINTRO = 0,
						MarkerMIX = 0,
						MarkerOUT = 0,
						IsScheduled = true
					};
				}

				if (scheduledItem != null)
				{
					scheduledItem.IsScheduled = true;
					ApplyScheduleVideoBuffer(scheduledItem);
					int insertPosition = GetCorrectScheduleInsertPosition();
					InsertItem(insertPosition, scheduledItem);
					Log($"[InsertScheduledAudioFile] ✅ Inserito: {scheduleName}");
				}
				else
				{
					Log($"[InsertScheduledAudioFile] ⚠️ Impossibile creare l'elemento per: {scheduleName}");
				}
			}
			catch (Exception ex)
			{
				Log($"[InsertScheduledAudioFile] Errore:  {ex.Message}");
			}
		}

		private void InsertScheduledMiniPlaylist(int miniPLSID)
		{
			try
			{
				var miniPlaylists = DbcManager.LoadFromCsv<MiniPLSEntry>("MiniPLS.dbc");
				var miniPLS = miniPlaylists.FirstOrDefault(m => m.ID == miniPLSID);

				if (miniPLS == null || string.IsNullOrEmpty(miniPLS.Items))
					return;

				var items = miniPLS.Items.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				var allMusic = DbcManager.LoadFromCsv<MusicEntry>("Music.dbc");
				var allClips = DbcManager.LoadFromCsv<ClipEntry>("Clips.dbc");

				int insertIndex = GetCorrectScheduleInsertPosition();
				int addedCount = 0;

				foreach (var itemStr in items)
				{
					var parts = itemStr.Split(':');
					if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int id))
					{
						string type = parts[0].Trim();
						PlaylistQueueItem queueItem = null;

						if (type == "Music")
						{
							var musicEntry = allMusic.FirstOrDefault(m => m.ID == id);
							if (musicEntry != null)
								queueItem = CreateMusicQueueItem(musicEntry);
						}
						else if (type == "Clip")
						{
							var clipEntry = allClips.FirstOrDefault(c => c.ID == id);
							if (clipEntry != null)
								queueItem = CreateClipQueueItem(clipEntry);
						}

						if (queueItem != null)
						{
							queueItem.IsScheduled = true;
							ApplyScheduleVideoBuffer(queueItem);
							InsertItemBatch(insertIndex, queueItem);
							insertIndex++;
							addedCount++;
						}
					}
				}

				if (addedCount > 0)
					FinalizeBatchModification();
			}
			catch { }
		}

		public string GetCurrentClockName()
		{
			return _currentClockName;
		}

		public void ChangeClockManually(string newClockName)
		{
			ClearNonPlayingItems();
			_isInitialStartup = false;
			GeneratePlaylistFromClock(newClockName, 1000);
		}

		private class ClockItem
		{
			public string Type { get; set; }
			public string CategoryName { get; set; }
			public string Value { get; set; }
			public int Count { get; set; }
			public bool YearFilterEnabled { get; set; }
			public int YearFrom { get; set; }
			public int YearTo { get; set; }
		}

		public void ReloadTodaySchedules()
		{
			try
			{
				DateTime today = DateTime.Now;
				int currentDayOfWeek = (int)today.DayOfWeek;

				var schedules = DbcManager.LoadFromCsv<ScheduleEntry>("Schedules.dbc");
				var todaySchedules = schedules.Where(s => s.IsEnabled == 1 && IsDayEnabled(s, currentDayOfWeek)).ToList();

				_executedSchedules.Clear();

				MessageBox.Show(string.Format(LanguageManager.GetString("Queue.SchedulesUpdated", "✅ Schedulazioni aggiornate!\n\n{0} schedulazioni attive per oggi."), todaySchedules.Count),
					LanguageManager.GetString("Queue.SchedulesUpdatedTitle", "Aggiornamento Schedulazioni"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show(string.Format(LanguageManager.GetString("Queue.UpdateError", "Errore:\n{0}"), ex.Message), LanguageManager.GetString("Common.Error", "Errore"), MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void FillQueueFromLastClock()
		{
			try
			{
				string lastClockName = DbcManager.GetConfigValue("LastUsedClock", "");

				if (string.IsNullOrEmpty(lastClockName))
				{
					var clocks = DbcManager.LoadFromCsv<ClockEntry>("Clocks.dbc");
					var defaultClock = clocks.FirstOrDefault(c => c.IsDefault == 1);

					if (defaultClock != null)
						lastClockName = defaultClock.ClockName;
					else if (clocks.Count > 0)
						lastClockName = clocks[0].ClockName;
				}

				if (!string.IsNullOrEmpty(lastClockName))
					GeneratePlaylistFromClock(lastClockName, 1000);
			}
			catch { }
		}

		public void GenerateInitialPlaylist()
		{
			try
			{
				bool autoStartEnabled = ConfigurationControl.GetAutoStartMode();

				if (!autoStartEnabled)
					return;

				_isInitialStartup = true;

				var schedules = DbcManager.LoadFromCsv<ScheduleEntry>("Schedules.dbc");
				var enabledSchedules = schedules.Where(s => s.IsEnabled == 1).ToList();

				ScheduleEntry lastClockSchedule = null;
				DateTime lastClockTime = DateTime.MinValue;
				DateTime now = DateTime.Now;
				int currentDayOfWeek = (int)now.DayOfWeek;

				foreach (var schedule in enabledSchedules)
				{
					if (schedule.Type != "PlayClock")
						continue;

					if (!IsDayEnabled(schedule, currentDayOfWeek))
						continue;

					var times = schedule.Times.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (var timeStr in times)
					{
						if (TimeSpan.TryParse(timeStr, out TimeSpan scheduleTime))
						{
							DateTime scheduleDateTime = now.Date.Add(scheduleTime);

							if (scheduleDateTime <= now && scheduleDateTime > lastClockTime)
							{
								lastClockSchedule = schedule;
								lastClockTime = scheduleDateTime;
							}
						}
					}
				}

				string clockName = "";

				if (lastClockSchedule != null)
				{
					clockName = lastClockSchedule.ClockName;
				}
				else
				{
					var clocks = DbcManager.LoadFromCsv<ClockEntry>("Clocks.dbc");
					var defaultClock = clocks.FirstOrDefault(c => c.IsDefault == 1);

					if (defaultClock != null)
						clockName = defaultClock.ClockName;
					else if (clocks.Count > 0)
						clockName = clocks[0].ClockName;
				}

				if (!string.IsNullOrEmpty(clockName))
					GeneratePlaylistFromClock(clockName, 1000);
			}
			catch { }
		}

        // ═══════════════════════════════════════════════════════════
        // METODO GeneratePlaylistFromClock - CORRETTO
        // ═══════════════════════════════════════════════════════════

        public async void GeneratePlaylistFromClock(string clockName, int maxItems = 1000)
        {
            if (_isGeneratingClock)
            {
                Log($"[GenerateClock] ⚠️ Generazione già in corso, richiesta ignorata");
                return;
            }

            _isGeneratingClock = true;

            try
            {
                Log($"");
                Log($"╔════════════════════════════════════════════════════════════╗");
                Log($"║  🎼 GENERAZIONE PLAYLIST DA CLOCK: {clockName,-30} ║");
                Log($"╚════════════════════════════════════════════════════════════╝");

                _queueSnapshotAtCreation.Clear();
                DateTime now = DateTime.Now;
                foreach (var item in _items)
                {
                    if (item.Type == PlaylistItemType.Music && !string.IsNullOrEmpty(item.Artist))
                    {
                        string key = $"{item.Artist}|||{item.Title}";
                        if (!_queueSnapshotAtCreation.ContainsKey(key))
                            _queueSnapshotAtCreation[key] = item.ScheduledTime;
                    }
                }

                string pendingVideoBuffer = _pendingScheduleVideoBufferPath;

                _generatingBatch = new List<PlaylistQueueItem>();

                const int earlyBatchSize = 5;
                var earlyBatchReady = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                var generationTask = Task.Run(() =>
                {
                    int addedCount = 0;
                    int totalAttempts = 0;

                    var clocks = DbcManager.LoadFromCsv<ClockEntry>("Clocks.dbc");
                    var clock = clocks.FirstOrDefault(c => c.ClockName == clockName);

                    if (clock == null)
                    {
                        Log($"[GenerateClock] ❌ Clock '{clockName}' non trovato!");
                        earlyBatchReady.TrySetResult(true);
                        return (addedCount, totalAttempts);
                    }

                    if (string.IsNullOrEmpty(clock.Items))
                    {
                        Log($"[GenerateClock] ⚠️ Clock '{clockName}' vuoto!");
                        earlyBatchReady.TrySetResult(true);
                        return (addedCount, totalAttempts);
                    }

                    List<ClockItem> clockItems;
                    try
                    {
                        clockItems = JsonConvert.DeserializeObject<List<ClockItem>>(clock.Items);
                        Log($"[GenerateClock] 📋 Clock contiene {clockItems.Count} elementi");
                    }
                    catch (Exception jsonEx)
                    {
                        Log($"[GenerateClock] ❌ Errore parsing JSON: {jsonEx.Message}");
                        earlyBatchReady.TrySetResult(true);
                        return (addedCount, totalAttempts);
                    }

                    int maxAttemptsPerItem = 10;

                    for (int clockIndex = 0; clockIndex < clockItems.Count; clockIndex++)
                    {
                        ClockItem currentItem = clockItems[clockIndex];

                        Log($"");
                        Log($"[GenerateClock] ━━━ Elemento #{clockIndex + 1}/{clockItems.Count} ━━━");
                        Log($"[GenerateClock] Tipo:   {currentItem.Type}");
                        Log($"[GenerateClock] Valore: {currentItem.Value ?? currentItem.CategoryName}");
                        Log($"[GenerateClock] Count: {currentItem.Count}");

                        int itemsToAdd = currentItem.Count;
                        int addedForThisItem = 0;

                        for (int i = 0; i < itemsToAdd && totalAttempts < maxItems * 3; i++)
                        {
                            PlaylistQueueItem queueItem = null;
                            int attempts = 0;

                            while (queueItem == null && attempts < maxAttemptsPerItem)
                            {
                                // ✅ MUSIC CATEGORY
                                if (currentItem.Type == "Music_Category")
                                {
                                    Log($"[GenerateClock]   → Cerco Music_Category:   {currentItem.CategoryName}");
                                    queueItem = GetRandomItemByCategory(currentItem.CategoryName, "Music",
                                        currentItem.YearFilterEnabled, currentItem.YearFrom, currentItem.YearTo);
                                }
                                // ✅ CLIPS CATEGORY
                                else if (currentItem.Type == "Clips_Category")
                                {
                                    Log($"[GenerateClock]   → Cerco Clips_Category:  {currentItem.CategoryName}");
                                    queueItem = GetRandomItemByCategory(currentItem.CategoryName, "Clip",
                                        currentItem.YearFilterEnabled, currentItem.YearFrom, currentItem.YearTo);
                                }
                                // ✅ MUSIC GENRE
                                else if (currentItem.Type == "Music_Genre")
                                {
                                    Log($"[GenerateClock]   → Cerco Music_Genre: {currentItem.Value}");
                                    queueItem = GetRandomMusicByGenre(currentItem.Value,
                                        currentItem.YearFilterEnabled, currentItem.YearFrom, currentItem.YearTo);
                                }
                                // ✅ CLIPS GENRE (ERA MANCANTE!)
                                else if (currentItem.Type == "Clips_Genre")
                                {
                                    Log($"[GenerateClock]   → Cerco Clips_Genre:  {currentItem.Value}");
                                    queueItem = GetRandomClipByGenre(currentItem.Value);
                                }
                                // ✅ MUSIC CATEGORY+GENRE
                                else if (currentItem.Type == "Music_Category+Genre")
                                {
                                    Log($"[GenerateClock]   → Cerco Music_Category+Genre: {currentItem.Value}");
                                    queueItem = GetRandomMusicByCategoryAndGenre(currentItem.Value,
                                        currentItem.YearFilterEnabled, currentItem.YearFrom, currentItem.YearTo);
                                }
                                // ✅ CLIPS CATEGORY+GENRE
                                else if (currentItem.Type == "Clips_Category+Genre")
                                {
                                    Log($"[GenerateClock]   → Cerco Clips_Category+Genre: {currentItem.Value}");
                                    queueItem = GetRandomClipByCategoryAndGenre(currentItem.Value);
                                }
                                else
                                {
                                    Log($"[GenerateClock]   ⚠️ Tipo sconosciuto: {currentItem.Type}");
                                }

                                attempts++;
                                totalAttempts++;

                                if (queueItem != null)
                                {
                                    Log($"[GenerateClock]   ✅ Trovato dopo {attempts} tentativi");
                                }
                            }

                            if (queueItem != null)
                            {
                                if (!string.IsNullOrEmpty(pendingVideoBuffer))
                                    ApplyScheduleVideoBuffer(queueItem);
                                lock (_generatingBatchLock) { _generatingBatch.Add(queueItem); }
                                addedCount++;
                                addedForThisItem++;

                                if (addedCount == earlyBatchSize)
                                    earlyBatchReady.TrySetResult(true);
                            }
                            else
                            {
                                Log($"[GenerateClock]   ❌ Nessun elemento trovato dopo {maxAttemptsPerItem} tentativi");
                            }
                        }

                        Log($"[GenerateClock] ✅ Aggiunti {addedForThisItem}/{itemsToAdd} per questo elemento");
                    }

                    earlyBatchReady.TrySetResult(true);
                    return (addedCount, totalAttempts);
                });

                // Wait for the first batch to be ready so playback can start immediately
                await earlyBatchReady.Task;

                List<PlaylistQueueItem> earlyItems;
                lock (_generatingBatchLock) { earlyItems = new List<PlaylistQueueItem>(_generatingBatch); }
                int earlyCount = earlyItems.Count;

                if (earlyCount > 0)
                {
                    Log($"[GenerateClock] ⚡ Primi {earlyCount} elementi pronti, aggiungo subito alla coda");
                    foreach (var item in earlyItems)
                        AddItemBatch(item);
                    FinalizeBatchModification();
                    Log($"[GenerateClock] 📢 ItemsAdded (early batch, {_items.Count} in coda)");
                    ItemsAdded?.Invoke(this, EventArgs.Empty);
                }

                // Fire QueueReady early so playback can start while generation continues
                bool earlyQueueReadyFired = false;
                if (_isInitialStartup && _items.Count >= 1)
                {
                    Log($"[GenerateClock] ▶️ QueueReady anticipato con {_items.Count} elementi");
                    QueueReady?.Invoke(this, _items.Count);
                    earlyQueueReadyFired = true;
                }

                // Wait for the full generation to complete
                var result = await generationTask;

                // Add remaining items that were generated after the early batch
                List<PlaylistQueueItem> remainingItems;
                lock (_generatingBatchLock)
                {
                    remainingItems = _generatingBatch.Count > earlyCount
                        ? _generatingBatch.GetRange(earlyCount, _generatingBatch.Count - earlyCount)
                        : new List<PlaylistQueueItem>();
                }

                if (remainingItems.Count > 0)
                {
                    Log($"[GenerateClock] 📦 Aggiungo i restanti {remainingItems.Count} elementi alla coda");
                    int batchCount = 0;
                    const int batchSize = 5;
                    foreach (var item in remainingItems)
                    {
                        AddItemBatch(item);
                        batchCount++;

                        if (batchCount >= batchSize)
                        {
                            FinalizeBatchModification();
                            batchCount = 0;
                            await Task.Yield();
                        }
                    }

                    if (batchCount > 0)
                        FinalizeBatchModification();

                    Log($"[GenerateClock] 📢 ItemsAdded (remaining batch, {_items.Count} in coda)");
                    ItemsAdded?.Invoke(this, EventArgs.Empty);
                }

                _currentClockName = clockName;
                DbcManager.SetConfigValue("LastUsedClock", clockName);
                ClockChanged?.Invoke(this, clockName);

                Log($"");
                Log($"[GenerateClock] ═══════════════════════════════════════════");
                Log($"[GenerateClock] ✅ GENERAZIONE COMPLETATA");
                Log($"[GenerateClock] Totale aggiunti: {result.addedCount}");
                Log($"[GenerateClock] Totale tentativi: {result.totalAttempts}");
                Log($"[GenerateClock] ═══════════════════════════════════════════");
                Log($"");

                if (!earlyQueueReadyFired && _isInitialStartup && _items.Count >= 1)
                    QueueReady?.Invoke(this, _items.Count);
            }
            catch (Exception ex)
            {
                Log($"[GenerateClock] ❌ ERRORE CRITICO: {ex.Message}");
                Log($"[GenerateClock] StackTrace: {ex.StackTrace}");
            }
            finally
            {
                _generatingBatch = null;
                _isGeneratingClock = false;
            }
        }

        // ✅ NUOVO:  Clips per genere
        private PlaylistQueueItem GetRandomClipByGenre(string genre)
        {
            try
            {
                Log($"[GetRandomClipByGenre] 🔍 Cerco clips con genere '{genre}'");

                var allClips = DbcManager.LoadFromCsv<ClipEntry>("Clips.dbc");
                var filtered = allClips.Where(c => c.Genre == genre).ToList();

                Log($"[GetRandomClipByGenre] Trovati {filtered.Count} clips totali con genere '{genre}'");

                var valid = filtered.Where(c => IsItemValid(c)).ToList();

                Log($"[GetRandomClipByGenre] {valid.Count} clips validi (validità temporale OK)");

                if (valid.Count == 0)
                {
                    Log($"[GetRandomClipByGenre] ❌ Nessun clip valido");
                    return null;
                }

                Random rnd = new Random(Guid.NewGuid().GetHashCode());
                var selected = valid[rnd.Next(valid.Count)];

                Log($"[GetRandomClipByGenre] ✅ Selezionato: {selected.Title}");

                return CreateClipQueueItem(selected);
            }
            catch (Exception ex)
            {
                Log($"[GetRandomClipByGenre] ❌ Errore: {ex.Message}");
                return null;
            }
        }

        // ✅ NUOVO: Music per categoria+genere
        private PlaylistQueueItem GetRandomMusicByCategoryAndGenre(string value, bool yearFilter, int yearFrom, int yearTo)
        {
            try
            {
                var parts = value.Split(new[] { " + " }, StringSplitOptions.None);
                if (parts.Length != 2)
                {
                    Log($"[GetRandomMusicByCategoryAndGenre] ⚠️ Formato non valido: '{value}'");
                    return null;
                }

                string category = parts[0].Trim();
                string genre = parts[1].Trim();

                Log($"[GetRandomMusicByCategoryAndGenre] 🔍 Cerco Music con Categoria='{category}' E Genere='{genre}'");

                var allMusic = DbcManager.LoadFromCsv<MusicEntry>("Music.dbc");

                var filtered = allMusic.Where(m =>
                    (m.Categories ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Any(c => c.Trim().Equals(category, StringComparison.OrdinalIgnoreCase)) &&
                    m.Genre == genre).ToList();

                Log($"[GetRandomMusicByCategoryAndGenre] Trovati {filtered.Count} brani con categoria+genere");

                if (yearFilter)
                {
                    filtered = filtered.Where(m => m.Year >= yearFrom && m.Year <= yearTo).ToList();
                    Log($"[GetRandomMusicByCategoryAndGenre] Filtro anni {yearFrom}-{yearTo}:  rimangono {filtered.Count} brani");
                }

                var valid = filtered.Where(m => IsItemValid(m)).ToList();
                Log($"[GetRandomMusicByCategoryAndGenre] {valid.Count} brani validi");

                if (valid.Count == 0)
                {
                    Log($"[GetRandomMusicByCategoryAndGenre] ❌ Nessun brano valido");
                    return null;
                }

                var available = valid.Where(m => CanPlayMusic(m) && !ShouldSkipArtistForAlternation(m.Artist)).ToList();

                if (available.Count > 0)
                {
                    Random rnd = new Random(Guid.NewGuid().GetHashCode());
                    var selected = available[rnd.Next(available.Count)];
                    _lastArtistFirstLetter = selected.Artist;
                    Log($"[GetRandomMusicByCategoryAndGenre] ✅ Selezionato (perfetto): {selected.Artist} - {selected.Title}");
                    return CreateMusicQueueItemWithEntry(selected, selected);
                }

                Log($"[GetRandomMusicByCategoryAndGenre] ⚠️ Applico FALLBACK");
                return GetBestAvailableMusic(valid, "category+genre", $"{category}+{genre}");
            }
            catch (Exception ex)
            {
                Log($"[GetRandomMusicByCategoryAndGenre] ❌ Errore: {ex.Message}");
                return null;
            }
        }

        // ✅ NUOVO: Clips per categoria+genere
        private PlaylistQueueItem GetRandomClipByCategoryAndGenre(string value)
        {
            try
            {
                var parts = value.Split(new[] { " + " }, StringSplitOptions.None);
                if (parts.Length != 2)
                {
                    Log($"[GetRandomClipByCategoryAndGenre] ⚠️ Formato non valido: '{value}'");
                    return null;
                }

                string category = parts[0].Trim();
                string genre = parts[1].Trim();

                Log($"[GetRandomClipByCategoryAndGenre] 🔍 Cerco Clips con Categoria='{category}' E Genere='{genre}'");

                var allClips = DbcManager.LoadFromCsv<ClipEntry>("Clips.dbc");

                var filtered = allClips.Where(c =>
                    (c.Categories ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Any(cat => cat.Trim().Equals(category, StringComparison.OrdinalIgnoreCase)) &&
                    c.Genre == genre).ToList();

                Log($"[GetRandomClipByCategoryAndGenre] Trovati {filtered.Count} clips con categoria+genere");

                var valid = filtered.Where(c => IsItemValid(c)).ToList();
                Log($"[GetRandomClipByCategoryAndGenre] {valid.Count} clips validi");

                if (valid.Count == 0)
                {
                    Log($"[GetRandomClipByCategoryAndGenre] ❌ Nessun clip valido");
                    return null;
                }

                Random rnd = new Random(Guid.NewGuid().GetHashCode());
                var selected = valid[rnd.Next(valid.Count)];

                Log($"[GetRandomClipByCategoryAndGenre] ✅ Selezionato: {selected.Title}");

                return CreateClipQueueItem(selected);
            }
            catch (Exception ex)
            {
                Log($"[GetRandomClipByCategoryAndGenre] ❌ Errore: {ex.Message}");
                return null;
            }
        }

        // (CONTINUA CON TUTTI I METODI RIMANENTI IDENTICI AL TUO CODICE...)

        private List<string> GetViolations(MusicEntry entry)
        {
            List<string> violations = new List<string>();

            try
            {
                int artistSeparationHours = ConfigurationControl.GetArtistSeparation();
                int hourlySeparationHours = ConfigurationControl.GetHourlySeparation();

                DateTime now = DateTime.Now;
                DateTime limitArtist = now.AddHours(-artistSeparationHours);
                DateTime limitTrack = now.AddHours(-hourlySeparationHours);

                var recentReports = ReportManager.LoadReport(limitTrack, now);

                // Controllo separazione brano
                foreach (var report in recentReports)
                {
                    if (report.Type == "Music" && report.Artist == entry.Artist && report.Title == entry.Title)
                    {
                        if (DateTime.TryParse($"{report.Date: yyyy-MM-dd} {report.StartTime}", out DateTime playedTime))
                        {
                            if (playedTime >= limitTrack)
                            {
                                violations.Add(LanguageManager.GetString("Queue.TrackSeparation", "Sep.Brano")); // ? CORRETTO
                                break;
                            }
                        }
                    }
                }

                // Controllo separazione artista
                string trackSepKey = LanguageManager.GetString("Queue.TrackSeparation", "Sep.Brano");
                if (!violations.Contains(trackSepKey))
                {
                    foreach (var report in recentReports)
                    {
                        if (report.Type == "Music" && report.Artist == entry.Artist)
                        {
                            if (DateTime.TryParse($"{report.Date:yyyy-MM-dd} {report.StartTime}", out DateTime playedTime))
                            {
                                if (playedTime >= limitArtist)
                                {
                                    violations.Add(LanguageManager.GetString("Queue.ArtistSeparation", "Sep. Artista")); // ? CORRETTO
                                    break;
                                }
                            }
                        }
                    }
                }

                // Controllo presenza in coda
                string trackKey = $"{entry.Artist}|||{entry.Title}";
                if (_queueSnapshotAtCreation.ContainsKey(trackKey))
                {
                    violations.Add(LanguageManager.GetString("Queue.AlreadyInQueue", "Già in coda")); // ? CORRETTO
                }
                else
                {
                    // Controllo artista vicino in coda
                    foreach (var kvp in _queueSnapshotAtCreation)
                    {
                        string[] parts = kvp.Key.Split(new[] { "|||" }, StringSplitOptions.None);
                        if (parts.Length == 2 && parts[0] == entry.Artist)
                        {
                            TimeSpan timeDiff = kvp.Value - now;
                            if (timeDiff.TotalHours < artistSeparationHours && timeDiff.TotalHours >= 0)
                            {
                                string artistSepKey = LanguageManager.GetString("Queue.ArtistSeparation", "Sep.Artista");
                                if (!violations.Contains(artistSepKey))
                                {
                                    violations.Add(LanguageManager.GetString("Queue.ArtistNearby", "Artista vicino")); // ? CORRETTO
                                }
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"[GetViolations] Errore:  {ex.Message}");
            }

            return violations;
        }

        private bool CanPlayMusic(MusicEntry entry)
		{
			try
			{
				int artistSeparationHours = ConfigurationControl.GetArtistSeparation();
				int hourlySeparationHours = ConfigurationControl.GetHourlySeparation();

				DateTime now = DateTime.Now;
				DateTime limitArtist = now.AddHours(-artistSeparationHours);
				DateTime limitTrack = now.AddHours(-hourlySeparationHours);

				var recentReports = ReportManager.LoadReport(limitTrack, now);

				foreach (var report in recentReports)
				{
					if (report.Type == "Music")
					{
						if (report.Artist == entry.Artist && report.Title == entry.Title)
						{
							if (DateTime.TryParse($"{report.Date:yyyy-MM-dd} {report.StartTime}", out DateTime playedTime))
							{
								if (playedTime >= limitTrack)
									return false;
							}
						}

						if (report.Artist == entry.Artist)
						{
							if (DateTime.TryParse($"{report.Date:yyyy-MM-dd} {report.StartTime}", out DateTime playedTime))
							{
								if (playedTime >= limitArtist)
									return false;
							}
						}
					}
				}

				foreach (var item in _items)
				{
					if (item.Type == PlaylistItemType.Music)
					{
						if (item.Artist == entry.Artist && item.Title == entry.Title)
							return false;

						if (item.Artist == entry.Artist)
						{
							TimeSpan timeDiff = item.ScheduledTime - now;

							if (timeDiff.TotalHours < artistSeparationHours)
								return false;
						}
					}
				}

				var genBatch = _generatingBatch;
				if (genBatch != null)
				{
					List<PlaylistQueueItem> snapshot;
					lock (_generatingBatchLock) { snapshot = genBatch.ToList(); }
					foreach (var item in snapshot)
					{
						if (item.Type == PlaylistItemType.Music)
						{
							if (item.Artist == entry.Artist && item.Title == entry.Title)
								return false;

							if (item.Artist == entry.Artist)
								return false;
						}
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				Log($"[CanPlayMusic] Errore: {ex.Message}");
				return true;
			}
		}

		private bool ShouldSkipArtistForAlternation(string artistName)
		{
			if (string.IsNullOrEmpty(_lastArtistFirstLetter))
				return false;

			if (string.IsNullOrEmpty(artistName))
				return false;

			char currentFirstLetter = char.ToUpper(artistName[0]);
			char lastFirstLetter = char.ToUpper(_lastArtistFirstLetter[0]);

			bool currentIsLetter = char.IsLetter(currentFirstLetter);
			bool lastIsLetter = char.IsLetter(lastFirstLetter);

			if (!currentIsLetter || !lastIsLetter)
				return false;

			int distance = Math.Abs(currentFirstLetter - lastFirstLetter);

			if (distance < 5)
				return true;

			return false;
		}

		private PlaylistQueueItem GetRandomMusicByGenre(string genre, bool yearFilter, int yearFrom, int yearTo)
		{
			try
			{
				var allMusic = DbcManager.LoadFromCsv<MusicEntry>("Music.dbc");
				var filtered = allMusic.Where(m => m.Genre == genre).ToList();

				if (yearFilter)
					filtered = filtered.Where(m => m.Year >= yearFrom && m.Year <= yearTo).ToList();

				var valid = filtered.Where(m => IsItemValid(m)).ToList();

				if (valid.Count == 0)
					return null;

				var available = valid.Where(m => CanPlayMusic(m) && !ShouldSkipArtistForAlternation(m.Artist)).ToList();

				if (available.Count > 0)
				{
					Random rnd = new Random(Guid.NewGuid().GetHashCode());
					var selected = available[rnd.Next(available.Count)];

					_lastArtistFirstLetter = selected.Artist;

					return CreateMusicQueueItemWithEntry(selected, selected);
				}

				return GetBestAvailableMusic(valid, "genre", genre);
			}
			catch (Exception ex)
			{
				Log($"[GetRandomMusicByGenre] Errore: {ex.Message}");
				return null;
			}
		}

		private PlaylistQueueItem GetRandomItemByCategory(string category, string type, bool yearFilter, int yearFrom, int yearTo)
		{
			try
			{
				if (type == "Music")
				{
					var allMusic = DbcManager.LoadFromCsv<MusicEntry>("Music.dbc");
					var filtered = allMusic.Where(m =>
						(m.Categories ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
						.Any(c => c.Trim().Equals(category, StringComparison.OrdinalIgnoreCase))).ToList();

					if (yearFilter)
						filtered = filtered.Where(m => m.Year >= yearFrom && m.Year <= yearTo).ToList();

					var valid = filtered.Where(m => IsItemValid(m)).ToList();

					if (valid.Count == 0)
						return null;

					var available = valid.Where(m => CanPlayMusic(m) && !ShouldSkipArtistForAlternation(m.Artist)).ToList();

					if (available.Count > 0)
					{
						Random rnd = new Random(Guid.NewGuid().GetHashCode());
						var selected = available[rnd.Next(available.Count)];

						_lastArtistFirstLetter = selected.Artist;

						return CreateMusicQueueItemWithEntry(selected, selected);
					}

					return GetBestAvailableMusic(valid, "category", category);
				}
				else if (type == "Clip")
				{
					var allClips = DbcManager.LoadFromCsv<ClipEntry>("Clips.dbc");
					var filtered = allClips.Where(c =>
						(c.Categories ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
						.Any(cat => cat.Trim().Equals(category, StringComparison.OrdinalIgnoreCase))).ToList();

					var valid = filtered.Where(c => IsItemValid(c)).ToList();

					if (valid.Count == 0)
						return null;

					Random rnd = new Random(Guid.NewGuid().GetHashCode());
					return CreateClipQueueItem(valid[rnd.Next(valid.Count)]);
				}

				return null;
			}
			catch (Exception ex)
			{
				Log($"[GetRandomItemByCategory] Errore: {ex.Message}");
				return null;
			}
		}

		private PlaylistQueueItem GetBestAvailableMusic(List<MusicEntry> validTracks, string sourceType, string sourceName)
		{
			try
			{
				if (validTracks.Count == 0)
					return null;

				DateTime now = DateTime.Now;
				int artistSeparationHours = ConfigurationControl.GetArtistSeparation();
				int hourlySeparationHours = ConfigurationControl.GetHourlySeparation();
				DateTime limitArtist = now.AddHours(-artistSeparationHours);
				DateTime limitTrack = now.AddHours(-hourlySeparationHours);

				var recentReports = ReportManager.LoadReport(limitTrack.AddHours(-24), now);

				var artistsInQueue = new HashSet<string>(
					_items.Where(i => i.Type == PlaylistItemType.Music && !string.IsNullOrEmpty(i.Artist))
						  .Select(i => i.Artist),
					StringComparer.OrdinalIgnoreCase
				);

				var genBatch = _generatingBatch;
				List<PlaylistQueueItem> genBatchSnapshot = null;
				if (genBatch != null)
				{
					lock (_generatingBatchLock) { genBatchSnapshot = genBatch.ToList(); }
					foreach (var gi in genBatchSnapshot)
					{
						if (gi.Type == PlaylistItemType.Music && !string.IsNullOrEmpty(gi.Artist))
							artistsInQueue.Add(gi.Artist);
				 	}
				}

				var scoredTracks = new List<(MusicEntry track, int penaltyScore, DateTime? lastPlayed)>();

				foreach (var track in validTracks)
				{
					int penalty = 0;
					DateTime? lastPlayedTime = null;
					bool alreadyInQueue = false;

					if (!string.IsNullOrEmpty(_lastArtistFirstLetter) && !string.IsNullOrEmpty(track.Artist))
					{
						char currentFirstLetter = char.ToUpper(track.Artist[0]);
						char lastFirstLetter = char.ToUpper(_lastArtistFirstLetter[0]);

						if (char.IsLetter(currentFirstLetter) && char.IsLetter(lastFirstLetter))
						{
							int distance = Math.Abs(currentFirstLetter - lastFirstLetter);

							if (distance < 5)
								penalty += (5 - distance) * 800;
						}
					}

					foreach (var queueItem in _items)
					{
						if (queueItem.Type == PlaylistItemType.Music)
						{
							if (queueItem.Artist == track.Artist && queueItem.Title == track.Title)
							{
								penalty += 100000;
								alreadyInQueue = true;
								break;
							}
						}
					}

					if (!alreadyInQueue && genBatchSnapshot != null)
					{
						foreach (var genItem in genBatchSnapshot)
						{
							if (genItem.Type == PlaylistItemType.Music)
							{
								if (genItem.Artist == track.Artist && genItem.Title == track.Title)
								{
									penalty += 100000;
									alreadyInQueue = true;
									break;
								}
							}
						}
					}

					if (alreadyInQueue)
					{
						scoredTracks.Add((track, penalty, null));
						continue;
					}

					if (artistsInQueue.Contains(track.Artist))
						penalty += 5000;

					var trackReports = recentReports.Where(r =>
						r.Type == "Music" &&
						r.Artist == track.Artist &&
						r.Title == track.Title).ToList();

					if (trackReports.Any())
					{
						var mostRecent = trackReports
							.Select(r => DateTime.TryParse($"{r.Date:yyyy-MM-dd} {r.StartTime}", out DateTime dt) ? dt : DateTime.MinValue)
							.Max();

						if (mostRecent != DateTime.MinValue)
						{
							lastPlayedTime = mostRecent;
							double hoursSincePlay = (now - mostRecent).TotalHours;

							if (hoursSincePlay < hourlySeparationHours)
								penalty += (int)((hourlySeparationHours - hoursSincePlay) * 100);
						}
					}

					var artistReports = recentReports.Where(r =>
						r.Type == "Music" &&
						r.Artist == track.Artist).ToList();

					if (artistReports.Any())
					{
						var mostRecentArtist = artistReports
							.Select(r => DateTime.TryParse($"{r.Date:yyyy-MM-dd} {r.StartTime}", out DateTime dt) ? dt : DateTime.MinValue)
							.Max();

						if (mostRecentArtist != DateTime.MinValue)
						{
							double hoursSinceArtist = (now - mostRecentArtist).TotalHours;

							if (hoursSinceArtist < artistSeparationHours)
								penalty += (int)((artistSeparationHours - hoursSinceArtist) * 50);
						}
					}

					scoredTracks.Add((track, penalty, lastPlayedTime));
				}

				var bestTrack = scoredTracks
					.OrderBy(t => t.penaltyScore)
					.ThenBy(t => t.lastPlayed ?? DateTime.MinValue)
					.ThenBy(t => Guid.NewGuid())
					.FirstOrDefault();

				if (bestTrack.track != null)
				{
					_lastArtistFirstLetter = bestTrack.track.Artist;
					return CreateMusicQueueItemWithEntry(bestTrack.track, bestTrack.track);
				}

				Random rnd = new Random(Guid.NewGuid().GetHashCode());
				var randomTrack = validTracks[rnd.Next(validTracks.Count)];
				_lastArtistFirstLetter = randomTrack.Artist;

				return CreateMusicQueueItemWithEntry(randomTrack, randomTrack);
			}
			catch (Exception ex)
			{
				Log($"[FALLBACK] ❌ ERRORE: {ex.Message}");

				if (validTracks.Count > 0)
				{
					Random rnd = new Random(Guid.NewGuid().GetHashCode());
					var track = validTracks[rnd.Next(validTracks.Count)];
					_lastArtistFirstLetter = track.Artist;

					return CreateMusicQueueItemWithEntry(track, track);
				}

				return null;
			}
		}

		private bool IsItemValid(MusicEntry entry)
		{
			DateTime now = DateTime.Now;

			if (!string.IsNullOrEmpty(entry.ValidFrom) && DateTime.TryParse(entry.ValidFrom, out DateTime validFrom))
			{
				if (now.Date < validFrom.Date)
					return false;
			}

			if (!string.IsNullOrEmpty(entry.ValidTo) && DateTime.TryParse(entry.ValidTo, out DateTime validTo))
			{
				if (now.Date > validTo.Date)
					return false;
			}

			if (!string.IsNullOrEmpty(entry.ValidMonths))
			{
				var validMonths = entry.ValidMonths.Split(';').Select(m => int.Parse(m.Trim())).ToList();
				if (!validMonths.Contains(now.Month))
					return false;
			}

			if (!string.IsNullOrEmpty(entry.ValidDays))
			{
				var validDays = entry.ValidDays.Split(';').Select(d => d.Trim()).ToList();
				string currentDay = now.DayOfWeek.ToString();
				if (!validDays.Contains(currentDay))
					return false;
			}

			if (!string.IsNullOrEmpty(entry.ValidHours))
			{
				var validHours = entry.ValidHours.Split(';').Select(h => int.Parse(h.Trim())).ToList();
				if (!validHours.Contains(now.Hour))
					return false;
			}

			return true;
		}

		private bool IsItemValid(ClipEntry entry)
		{
			DateTime now = DateTime.Now;

			if (!string.IsNullOrEmpty(entry.ValidFrom) && DateTime.TryParse(entry.ValidFrom, out DateTime validFrom))
			{
				if (now.Date < validFrom.Date)
					return false;
			}

			if (!string.IsNullOrEmpty(entry.ValidTo) && DateTime.TryParse(entry.ValidTo, out DateTime validTo))
			{
				if (now.Date > validTo.Date)
					return false;
			}

			if (!string.IsNullOrEmpty(entry.ValidMonths))
			{
				var validMonths = entry.ValidMonths.Split(';').Select(m => int.Parse(m.Trim())).ToList();
				if (!validMonths.Contains(now.Month))
					return false;
			}

			if (!string.IsNullOrEmpty(entry.ValidDays))
			{
				var validDays = entry.ValidDays.Split(';').Select(d => d.Trim()).ToList();
				string currentDay = now.DayOfWeek.ToString();
				if (!validDays.Contains(currentDay))
					return false;
			}

			if (!string.IsNullOrEmpty(entry.ValidHours))
			{
				var validHours = entry.ValidHours.Split(';').Select(h => int.Parse(h.Trim())).ToList();
				if (!validHours.Contains(now.Hour))
					return false;
			}

			return true;
		}

        private bool IsDayEnabled(ScheduleEntry schedule, int dayOfWeek)
        {
            try
            {
                // dayOfWeek:  0=Sunday, 1=Monday, 2=Tuesday, ... 6=Saturday

                switch (dayOfWeek)
                {
                    case 0: return schedule.Sunday == 1;
                    case 1: return schedule.Monday == 1;
                    case 2: return schedule.Tuesday == 1;
                    case 3: return schedule.Wednesday == 1;
                    case 4: return schedule.Thursday == 1;
                    case 5: return schedule.Friday == 1;
                    case 6: return schedule.Saturday == 1;
                    default: return false;
                }
            }
            catch (Exception ex)
            {
                Log($"[IsDayEnabled] ? Errore:   {ex.Message}");
                return false;
            }
        }


        private PlaylistQueueItem CreateMusicQueueItemWithEntry(MusicEntry entry, MusicEntry originalEntry)
        {
            int effectiveDurationMs = entry.MarkerMIX > entry.MarkerIN
                ? entry.MarkerMIX - entry.MarkerIN
                : entry.Duration - entry.MarkerIN;

            return new PlaylistQueueItem
            {
                Type = PlaylistItemType.Music,
                ScheduledTime = DateTime.Now,
                Artist = entry.Artist,
                Title = entry.Title,
                Year = entry.Year,
                Duration = TimeSpan.FromMilliseconds(effectiveDurationMs),
                Intro = TimeSpan.FromMilliseconds(Math.Max(0, entry.MarkerINTRO - entry.MarkerIN)),
                FilePath = entry.FilePath,
                MarkerIN = entry.MarkerIN,
                MarkerINTRO = entry.MarkerINTRO,
                MarkerMIX = entry.MarkerMIX,
                MarkerOUT = entry.MarkerOUT,
                FileDurationMs = entry.Duration,
                IsScheduled = false,
                OriginalMusicEntry = originalEntry,
                // ✅ Campi video - converti enum a string
                VideoFilePath = entry.VideoFilePath ?? "",
                VideoSource = entry.VideoSource.ToString(),  // Converti enum a string
                NDISourceName = entry.NDISourceName ?? ""
            };
        }

        private PlaylistQueueItem CreateMusicQueueItem(MusicEntry entry)
		{
			return CreateMusicQueueItemWithEntry(entry, entry);
		}

		private PlaylistQueueItem CreateClipQueueItem(ClipEntry entry)
		{
			int effectiveDurationMs = entry.MarkerMIX > entry.MarkerIN
				? entry.MarkerMIX - entry.MarkerIN
				: entry.Duration - entry.MarkerIN;

			return new PlaylistQueueItem
			{
				Type = PlaylistItemType.Clip,
				ScheduledTime = DateTime.Now,
				Artist = "",
				Title = entry.Title,
				Year = 0,
				Duration = TimeSpan.FromMilliseconds(effectiveDurationMs),
				Intro = TimeSpan.FromMilliseconds(Math.Max(0, entry.MarkerINTRO - entry.MarkerIN)),
				FilePath = entry.FilePath,
				MarkerIN = entry.MarkerIN,
				MarkerINTRO = entry.MarkerINTRO,
				MarkerMIX = entry.MarkerMIX,
				MarkerOUT = entry.MarkerOUT,
				FileDurationMs = entry.Duration,
				IsScheduled = false,
				VideoFilePath = entry.VideoFilePath ?? "",
				VideoSource = entry.VideoSource.ToString(),
				NDISourceName = entry.NDISourceName ?? ""
			};
		}

		private void PlaylistQueueControl_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				int index = GetItemIndexAtPoint(e.Location);

				if (index >= 0 && index < _items.Count)
				{
					Rectangle itemRect = GetItemRect(index);

					bool showPreview = _items[index].Type != PlaylistItemType.ADV;

					int buttonsArea = showPreview ? (BUTTON_WIDTH * 2 + 3) : (BUTTON_WIDTH + 1);
						int btnX = itemRect.Right - buttonsArea;

						if (showPreview)
						{
							Rectangle previewZone = new Rectangle(btnX + 1, itemRect.Y, BUTTON_WIDTH, ITEM_HEIGHT);
							if (previewZone.Contains(e.Location))
							{
								var item = _items[index];
								if (File.Exists(item.FilePath))
								{
									PreviewRequested?.Invoke(this, item.FilePath);
								}
								else
								{
									MessageBox.Show(
										LanguageManager.GetString("Queue.FileNotFound", "File non trovato!"),
										LanguageManager.GetString("Common.Error", "Errore"),
										MessageBoxButtons.OK, MessageBoxIcon.Error);
								}
								return;
							}
							btnX += BUTTON_WIDTH + 1;
						}

						Rectangle deleteZone = new Rectangle(btnX + 1, itemRect.Y, BUTTON_WIDTH - 1, ITEM_HEIGHT);
					if (deleteZone.Contains(e.Location))
					{
						// Slot playing e player non stoppato: delete disabilitato
						if (index == _currentPlayingIndex && !_isPlayerStopped)
							return;
						RemoveItem(index);
						return;
					}

					if (index == 0 && _currentPlayingIndex == 0)
						return;

					_dragSourceIndex = index;
					_dragStartPoint = e.Location;
					_isDragging = false;
				}
			}
		}

        private void PlaylistQueueControl_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                // Drag logic (existing)
                if (e.Button == MouseButtons.Left && _dragSourceIndex >= 0 && !_isDragging)
                {
                    if (Math.Abs(e.X - _dragStartPoint.X) > 5 || Math.Abs(e.Y - _dragStartPoint.Y) > 5)
                    {
                        Log($"[MouseMove] Inizio drag da index {_dragSourceIndex}");
                        Log($"[MouseMove] Item: '{_items[_dragSourceIndex].Title}'");

                        _isDragging = true;
                        this.Cursor = Cursors.Hand;

                        var result = this.DoDragDrop(_items[_dragSourceIndex], DragDropEffects.Move);

                        Log($"[MouseMove] DragDrop completato, result: {result}");
                        this.Cursor = Cursors.Default;
                    }
                    return;
                }

                // Hover tracking for buttons (when not dragging)
                if (e.Button == MouseButtons.None || e.Button != MouseButtons.Left)
                {
                    int oldHoverIndex = _hoverButtonIndex;
                    string oldHoverType = _hoverButtonType;
                    _hoverButtonIndex = -1;
                    _hoverButtonType = null;

                    int index = GetItemIndexAtPoint(e.Location);
                    if (index >= 0 && index < _items.Count)
                    {
                        bool showPreview = _items[index].Type != PlaylistItemType.ADV;

                        {
                            Rectangle itemRect = GetItemRect(index);
                            int buttonsArea = showPreview ? (BUTTON_WIDTH * 2 + 3) : (BUTTON_WIDTH + 1);
                            int btnX = itemRect.Right - buttonsArea;

                            if (showPreview)
                            {
                                Rectangle previewZone = new Rectangle(btnX + 1, itemRect.Y, BUTTON_WIDTH, ITEM_HEIGHT);
                                if (previewZone.Contains(e.Location))
                                {
                                    _hoverButtonIndex = index;
                                    _hoverButtonType = "preview";
                                }
                                btnX += BUTTON_WIDTH + 1;
                            }

                            if (_hoverButtonType == null)
                            {
                                Rectangle deleteZone = new Rectangle(btnX + 1, itemRect.Y, BUTTON_WIDTH - 1, ITEM_HEIGHT);
                                if (deleteZone.Contains(e.Location))
                                {
                                    // Suppress hover on disabled delete button (currently playing and not stopped)
                                    if (!(index == _currentPlayingIndex && !_isPlayerStopped))
                                    {
                                        _hoverButtonIndex = index;
                                        _hoverButtonType = "delete";
                                    }
                                }
                            }
                        }
                    }

                    if (_hoverButtonIndex >= 0)
                        this.Cursor = Cursors.Hand;
                    else
                        this.Cursor = Cursors.Default;

                    if (oldHoverIndex != _hoverButtonIndex || oldHoverType != _hoverButtonType)
                    {
                        if (_hoverButtonType == "preview")
                            _buttonToolTip.SetToolTip(this, "Preview");
                        else if (_hoverButtonType == "delete")
                            _buttonToolTip.SetToolTip(this, "Remove");
                        else
                            _buttonToolTip.SetToolTip(this, null);

                        this.Invalidate();
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"[MouseMove] Errore: {ex.Message}");
                this.Cursor = Cursors.Default;
            }
        }

        private void PlaylistQueueControl_MouseUp(object sender, MouseEventArgs e)
		{
			_dragSourceIndex = -1;
			_isDragging = false;
		}


		private int GetItemIndexAtPoint(Point location)
		{
			int yPos = -_scrollOffset;
			for (int i = 0; i < _items.Count; i++)
			{
				if (location.Y >= yPos && location.Y < yPos + ITEM_HEIGHT)
					return i;
				yPos += ITEM_HEIGHT + ITEM_MARGIN;
			}
			return -1;
		}

		private Rectangle GetItemRect(int index)
		{
			int width = this.Width - (_scrollBar.Visible ? _scrollBar.Width : 0) - 10;
			int yPos = -_scrollOffset + (index * (ITEM_HEIGHT + ITEM_MARGIN));
			return new Rectangle(5, yPos, width, ITEM_HEIGHT);
		}

		private void RecalculateScheduledTimes()
		{
			DateTime currentTime = DateTime.Now;

			for (int i = 0; i < _items.Count; i++)
			{
				if (i == _currentPlayingIndex)
				{
					currentTime = _items[i].ScheduledTime.Add(_items[i].Duration);
				}
				else if (i < _currentPlayingIndex)
				{
				}
				else
				{
					_items[i].ScheduledTime = currentTime;
					currentTime = currentTime.Add(_items[i].Duration);
				}
			}
		}

		private void UpdateScrollBar()
		{
			int totalHeight = _items.Count * (ITEM_HEIGHT + ITEM_MARGIN);
			int visibleHeight = this.Height;

			if (totalHeight > visibleHeight)
			{
				_scrollBar.Visible = true;
				_scrollBar.Maximum = totalHeight - visibleHeight + _scrollBar.LargeChange;
				_scrollBar.LargeChange = visibleHeight;
			}
			else
			{
				_scrollBar.Visible = false;
				_scrollOffset = 0;
			}

			this.Invalidate();
		}

		private void ScrollBar_Scroll(object sender, ScrollEventArgs e)
		{
			_scrollOffset = e.NewValue;
			this.Invalidate();
		}

		private void PlaylistQueueControl_MouseWheel(object sender, MouseEventArgs e)
		{
			if (_scrollBar.Visible)
			{
				int newValue = _scrollBar.Value - (e.Delta / 120) * 30;
				newValue = Math.Max(_scrollBar.Minimum, Math.Min(_scrollBar.Maximum - _scrollBar.LargeChange + 1, newValue));
				_scrollBar.Value = newValue;
				_scrollOffset = newValue;
				this.Invalidate();
			}
		}

		private void PlaylistQueueControl_Paint(object sender, PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

			int yPos = -_scrollOffset;
			int index = 0;

			foreach (var item in _items)
			{
				if (yPos + ITEM_HEIGHT + ITEM_MARGIN > 0 && yPos < this.Height)
				{
					DrawPlaylistItem(g, item, yPos, index, index == _currentPlayingIndex);
				}

				yPos += ITEM_HEIGHT + ITEM_MARGIN;
				index++;
			}
		}

		private GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
		{
			var path = new GraphicsPath();
			int d = radius * 2;
			path.AddArc(rect.X, rect.Y, d, d, 180, 90);
			path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
			path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
			path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
			path.CloseFigure();
			return path;
		}

		private string FormatDuration(TimeSpan duration)
		{
			if (duration.TotalHours >= 1)
				return $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
			else
				return $"{duration.Minutes:D2}:{duration.Seconds:D2}";
		}

		private void DrawPlaylistItem(Graphics g, PlaylistQueueItem item, int yPos, int index, bool isPlaying)
		{
			int totalWidth = this.Width - (_scrollBar.Visible ? _scrollBar.Width : 0) - 10;
			Rectangle fullRect = new Rectangle(5, yPos, totalWidth, ITEM_HEIGHT);

			bool showButtons = true;
			bool showPreview = item.Type != PlaylistItemType.ADV;

			int buttonsArea = 0;
			if (showPreview && showButtons)
				buttonsArea = BUTTON_WIDTH * 2 + 3;
			else if (showButtons)
				buttonsArea = BUTTON_WIDTH + 1;

			int leftPanelsWidth = SIDE_BAR_WIDTH + NUM_PANEL_WIDTH + ICON_PANEL_WIDTH;
			int contentWidth = totalWidth - leftPanelsWidth - buttonsArea;
			Rectangle contentRect = new Rectangle(fullRect.X + leftPanelsWidth, fullRect.Y, contentWidth, ITEM_HEIGHT);

			// ── COLORI ──
			Color bgColor, textColor, sideBarColor;

			if (item.Type == PlaylistItemType.ADV)
			{
				bgColor = Color.FromArgb(80, 25, 25);
				textColor = Color.White;
				sideBarColor = Color.FromArgb(220, 30, 30);
			}
			else if (isPlaying)
			{
				bgColor = Color.FromArgb(35, 35, 35);
				textColor = Color.White;
				sideBarColor = Color.FromArgb(76, 175, 80); // Verde StatePlaying
			}
			else if (item.IsScheduled)
			{
				bgColor = Color.FromArgb(60, 40, 85);
				textColor = Color.White;
				sideBarColor = Color.FromArgb(138, 43, 226);
			}
			else
			{
				switch (item.Type)
				{
					case PlaylistItemType.Music:
						bgColor = Color.FromArgb(75, 75, 40);
						textColor = Color.White;
						sideBarColor = Color.FromArgb(255, 215, 0);
						break;
					case PlaylistItemType.Clip:
						bgColor = Color.FromArgb(40, 55, 80);
						textColor = Color.White;
						sideBarColor = Color.FromArgb(0, 180, 255);
						break;
					default:
						bgColor = Color.FromArgb(55, 55, 55);
						textColor = Color.White;
						sideBarColor = Color.FromArgb(100, 100, 100);
						break;
				}
			}

			// ── OMBRA ──
			Rectangle shadowRect = new Rectangle(fullRect.X + 2, fullRect.Y + 2, fullRect.Width, fullRect.Height);
			using (var shadowPath = GetRoundedRectPath(shadowRect, CORNER_RADIUS))
			using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
			{
				g.FillPath(shadowBrush, shadowPath);
			}

			// ── SFONDO PRINCIPALE ──
			using (var mainPath = GetRoundedRectPath(fullRect, CORNER_RADIUS))
			{
				using (LinearGradientBrush bgBrush = new LinearGradientBrush(
					fullRect,
					bgColor,
					Color.FromArgb(
						Math.Max(0, bgColor.R - 15),
						Math.Max(0, bgColor.G - 15),
						Math.Max(0, bgColor.B - 15)),
					LinearGradientMode.Vertical))
				{
					g.FillPath(bgBrush, mainPath);
				}

				// Bordo: VERDE per playing, altrimenti colore sideBar con alpha 150
				Color borderColor;
				float borderWidth;
				if (isPlaying)
				{
					borderColor = Color.FromArgb(200, 76, 175, 80);
					borderWidth = 2f;
				}
				else
				{
					borderColor = Color.FromArgb(150, sideBarColor.R, sideBarColor.G, sideBarColor.B);
					borderWidth = 1.5f;
				}

				using (Pen borderPen = new Pen(borderColor, borderWidth))
				{
					g.DrawPath(borderPen, mainPath);
				}
			}

			// ── BARRA LATERALE SINISTRA ──
			Rectangle sideBar = new Rectangle(fullRect.X, fullRect.Y, SIDE_BAR_WIDTH, ITEM_HEIGHT);
			using (var sideClip = GetRoundedRectPath(fullRect, CORNER_RADIUS))
			{
				var oldClip = g.Clip;
				g.SetClip(sideClip, System.Drawing.Drawing2D.CombineMode.Intersect);
				using (SolidBrush sideBrush = new SolidBrush(sideBarColor))
				{
					g.FillRectangle(sideBrush, sideBar);
				}
				g.Clip = oldClip;
			}

			// ── PANNELLO NUMERO (riquadro separato) ──
			Rectangle numPanel = new Rectangle(fullRect.X + SIDE_BAR_WIDTH, fullRect.Y, NUM_PANEL_WIDTH, ITEM_HEIGHT);
			using (var clipPath = GetRoundedRectPath(fullRect, CORNER_RADIUS))
			{
				var oldClip = g.Clip;
				g.SetClip(clipPath, System.Drawing.Drawing2D.CombineMode.Intersect);
				using (SolidBrush numPanelBg = new SolidBrush(Color.FromArgb(25, 255, 255, 255)))
				{
					g.FillRectangle(numPanelBg, numPanel);
				}
				// Separatore destro
				using (Pen sepPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1))
				{
					g.DrawLine(sepPen, numPanel.Right, fullRect.Y + 6, numPanel.Right, fullRect.Bottom - 6);
				}
				g.Clip = oldClip;
			}

			using (Font numFont = new Font("Segoe UI", 20, FontStyle.Bold))
			using (SolidBrush numBrush = new SolidBrush(Color.FromArgb(180, textColor)))
			using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
			{
				g.DrawString($"{index + 1}", numFont, numBrush, numPanel, sf);
			}

			// ── PANNELLO ICONA (riquadro separato) ──
			Rectangle iconPanel = new Rectangle(numPanel.Right, fullRect.Y, ICON_PANEL_WIDTH, ITEM_HEIGHT);
			using (var clipPath = GetRoundedRectPath(fullRect, CORNER_RADIUS))
			{
				var oldClip = g.Clip;
				g.SetClip(clipPath, System.Drawing.Drawing2D.CombineMode.Intersect);
				using (SolidBrush iconPanelBg = new SolidBrush(Color.FromArgb(15, 255, 255, 255)))
				{
					g.FillRectangle(iconPanelBg, iconPanel);
				}
				// Separatore destro
				using (Pen sepPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1))
				{
					g.DrawLine(sepPen, iconPanel.Right, fullRect.Y + 6, iconPanel.Right, fullRect.Bottom - 6);
				}
				g.Clip = oldClip;
			}

			string icon;
			if (item.Type == PlaylistItemType.ADV)
				icon = "💲";
			else if (!string.IsNullOrEmpty(item.FilePath) &&
					 (item.FilePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
					  item.FilePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
				icon = "🌐";
			else if (item.Type == PlaylistItemType.Music)
				icon = "🎵";
			else
				icon = "⚡";

			using (Font iconFont = new Font("Segoe UI Emoji", 18, FontStyle.Regular))
			using (SolidBrush iconBrush = new SolidBrush(textColor))
			using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
			{
				g.DrawString(icon, iconFont, iconBrush, iconPanel, sf);
			}

			// ── CONTENUTO: Titolo (centrato verticalmente) ──
			int contentX = contentRect.X + CONTENT_PADDING;
			int centerY = contentRect.Y + (ITEM_HEIGHT / 2);

			// Titolo centrato verticalmente
			string titleText = string.IsNullOrEmpty(item.Artist)
				? item.Title
				: $"{item.Artist} - {item.Title}";
			if (item.Year > 0)
				titleText += $" ({item.Year})";

			int titleMaxWidth = contentRect.Right - contentX - CONTENT_PADDING - 10;

			using (Font titleFont = new Font("Segoe UI", 13, FontStyle.Bold))
			using (SolidBrush titleBrush = new SolidBrush(textColor))
			using (StringFormat titleFormat = new StringFormat
			{
				Trimming = StringTrimming.EllipsisCharacter,
				FormatFlags = StringFormatFlags.NoWrap,
				LineAlignment = StringAlignment.Center
			})
			{
				SizeF titleSize = g.MeasureString(titleText, titleFont);
				float titleY = centerY - (titleSize.Height / 2) - 5;
				RectangleF titleRect = new RectangleF(contentX, titleY, titleMaxWidth, titleSize.Height);
				g.DrawString(titleText, titleFont, titleBrush, titleRect, titleFormat);
			}

			// ── INTRO (9pt, alto a destra del contenuto) ──
			if (item.Type != PlaylistItemType.ADV)
			{
				using (Font introFont = new Font("Segoe UI", 9, FontStyle.Regular))
				using (SolidBrush introBrush = new SolidBrush(Color.FromArgb(180, textColor)))
				{
					string introText = $"INTRO: {item.Intro.TotalSeconds:F1}s";
					SizeF introSize = g.MeasureString(introText, introFont);
					g.DrawString(introText, introFont, introBrush,
						contentRect.Right - introSize.Width - CONTENT_PADDING, contentRect.Y + 6);
				}
			}

			// ── DURATA (11pt Bold, a destra, centrata verticalmente ma leggermente sotto) ──
			string durationText = $"⏱ {FormatDuration(item.Duration)}";

			using (Font durationFont = new Font("Segoe UI", 11, FontStyle.Bold))
			using (SolidBrush durationBrush = new SolidBrush(textColor))
			{
				SizeF durationSize = g.MeasureString(durationText, durationFont);
				float durationY = centerY - (durationSize.Height / 2) + 2;
				g.DrawString(durationText, durationFont, durationBrush,
					contentRect.Right - durationSize.Width - CONTENT_PADDING, durationY);
			}

			// ── RIGA BASSA: "OnAir: HH:mm:ss" + Badges ──
			int row3Y = contentRect.Bottom - 22;
			int infoX = contentX; // allineato col titolo

			using (Font timeFont = new Font("Segoe UI", 9, FontStyle.Bold))
			using (SolidBrush timeBrush = new SolidBrush(Color.FromArgb(200, textColor)))
			{
				string timeText = $"OnAir: {item.ScheduledTime:HH:mm:ss}";
				g.DrawString(timeText, timeFont, timeBrush, infoX, row3Y);
				SizeF timeSize = g.MeasureString(timeText, timeFont);
				infoX += (int)timeSize.Width + 6;
			}

			// Badge SCHEDULED (pillola)
			if (item.IsScheduled)
			{
				using (Font schedFont = new Font("Segoe UI", 7, FontStyle.Bold))
				{
					string schedText = "SCHEDULED";
					SizeF schedSize = g.MeasureString(schedText, schedFont);
					Rectangle badgeRect = new Rectangle(infoX, row3Y + 1, (int)schedSize.Width + 8, 15);

					using (var badgePath = GetRoundedRectPath(badgeRect, 7))
					using (SolidBrush badgeBg = new SolidBrush(Color.FromArgb(138, 43, 226)))
					{
						g.FillPath(badgeBg, badgePath);
					}

					using (SolidBrush badgeTextBrush = new SolidBrush(Color.White))
					using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
					{
						g.DrawString(schedText, schedFont, badgeTextBrush, badgeRect, sf);
					}

					infoX += badgeRect.Width + 6;
				}
			}

			// Badge PLAYLIST (pillola azzurra/teal)
			if (item.IsFromPlaylist)
			{
				using (Font plFont = new Font("Segoe UI", 7, FontStyle.Bold))
				{
					string plText = "PLAYLIST";
					SizeF plSize = g.MeasureString(plText, plFont);
					Rectangle plRect = new Rectangle(infoX, row3Y + 1, (int)plSize.Width + 8, 15);

					using (var plPath = GetRoundedRectPath(plRect, 7))
					using (SolidBrush plBg = new SolidBrush(Color.FromArgb(0, 150, 180)))
					{
						g.FillPath(plBg, plPath);
					}

					using (SolidBrush plTextBrush = new SolidBrush(Color.White))
					using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
					{
						g.DrawString(plText, plFont, plTextBrush, plRect, sf);
					}

					infoX += plRect.Width + 6;
				}
			}

			// Badge violazioni (pillola arancio)
			if (item.Type == PlaylistItemType.Music && item.OriginalMusicEntry != null)
			{
				var violations = GetViolations(item.OriginalMusicEntry);
				if (violations.Count > 0)
				{
					using (Font violFont = new Font("Segoe UI", 7, FontStyle.Bold))
					{
						string violText = "⚠ " + string.Join(", ", violations);
						SizeF violSize = g.MeasureString(violText, violFont);
						Rectangle violRect = new Rectangle(infoX, row3Y + 1, (int)violSize.Width + 8, 15);

						using (var violPath = GetRoundedRectPath(violRect, 7))
						using (SolidBrush violBg = new SolidBrush(Color.FromArgb(180, 200, 120, 0)))
						{
							g.FillPath(violBg, violPath);
						}

						using (SolidBrush violTextBrush = new SolidBrush(Color.White))
						using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
						{
							g.DrawString(violText, violFont, violTextBrush, violRect, sf);
						}
					}
				}
			}

			// ── BOTTONI FULL-HEIGHT con HOVER ──
			if (showButtons)
			{
				int btnX = fullRect.Right - buttonsArea;

				using (Pen sepPen = new Pen(Color.FromArgb(50, 255, 255, 255), 1))
				{
					g.DrawLine(sepPen, btnX, fullRect.Y + 6, btnX, fullRect.Bottom - 6);
				}

				if (showPreview)
				{
					Rectangle previewRect = new Rectangle(btnX + 1, fullRect.Y, BUTTON_WIDTH, ITEM_HEIGHT);

					bool isPreviewHover = (_hoverButtonIndex == index && _hoverButtonType == "preview");
					Color previewBgColor = isPreviewHover
						? Color.FromArgb(60, 0, 120, 255)
						: Color.FromArgb(30, 0, 120, 255);

					using (SolidBrush previewBg = new SolidBrush(previewBgColor))
					{
						using (var clipPath = GetRoundedRectPath(fullRect, CORNER_RADIUS))
						{
							var oldClip = g.Clip;
							g.SetClip(clipPath, System.Drawing.Drawing2D.CombineMode.Intersect);
							g.FillRectangle(previewBg, previewRect);
							g.Clip = oldClip;
						}
					}

					Color previewIconColor = isPreviewHover
						? Color.FromArgb(255, 0, 180, 255)
						: Color.FromArgb(200, 0, 150, 255);

					using (Font previewFont = new Font("Segoe UI", 16, FontStyle.Bold))
					using (SolidBrush previewTextBrush = new SolidBrush(previewIconColor))
					using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
					{
						g.DrawString("▶", previewFont, previewTextBrush, previewRect, sf);
					}

					btnX += BUTTON_WIDTH + 1;

					using (Pen sepPen2 = new Pen(Color.FromArgb(40, 255, 255, 255), 1))
					{
						g.DrawLine(sepPen2, btnX, fullRect.Y + 6, btnX, fullRect.Bottom - 6);
					}
				}

				Rectangle deleteRect = new Rectangle(btnX + 1, fullRect.Y, BUTTON_WIDTH - 1, ITEM_HEIGHT);

				bool isDeleteDisabled = (index == _currentPlayingIndex && !_isPlayerStopped);
				bool isDeleteHover = !isDeleteDisabled && (_hoverButtonIndex == index && _hoverButtonType == "delete");
				Color deleteBgColor = isDeleteDisabled
					? Color.FromArgb(15, 255, 255, 255)
					: (isDeleteHover
						? Color.FromArgb(60, 255, 0, 0)
						: Color.FromArgb(30, 255, 0, 0));

				using (SolidBrush deleteBg = new SolidBrush(deleteBgColor))
				{
					using (var clipPath = GetRoundedRectPath(fullRect, CORNER_RADIUS))
					{
						var oldClip = g.Clip;
						g.SetClip(clipPath, System.Drawing.Drawing2D.CombineMode.Intersect);
						g.FillRectangle(deleteBg, deleteRect);
						g.Clip = oldClip;
					}
				}

				Color deleteIconColor = isDeleteDisabled
					? Color.FromArgb(80, 255, 255, 255)
					: (isDeleteHover
						? Color.FromArgb(255, 255, 80, 80)
						: Color.FromArgb(200, 255, 60, 60));

				using (Font deleteFont = new Font("Segoe UI", 14, FontStyle.Bold))
				using (SolidBrush deleteTextBrush = new SolidBrush(deleteIconColor))
				using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
				{
					g.DrawString("✖", deleteFont, deleteTextBrush, deleteRect, sf);
				}
			}
		}

		public void SetPlayerStopped(bool isStopped)
		{
			_isPlayerStopped = isStopped;
			this.Invalidate();
		}

		public void SetCurrentPlaying(int index)
		{
			if (_lastPlayedForReport != null)
			{
				try
				{
					ReportManager.LogTrack(
						_lastPlayedForReport.ItemType,
						_lastPlayedForReport.Artist,
						_lastPlayedForReport.Title,
						_lastPlayedForReport.ActualStartTime,
						DateTime.Now,
						_lastPlayedForReport.Duration
					);

					ReportUpdated?.Invoke(this, EventArgs.Empty);
				}
				catch (Exception ex)
				{
					Log($"[PlaylistQueue] Errore report: {ex.Message}");
				}
			}

			_currentPlayingIndex = index;

			if (index >= 0 && index < _items.Count)
			{
				_items[index].ScheduledTime = DateTime.Now;
				_items[index].ActualStartTime = DateTime.Now;

				_lastPlayedForReport = _items[index];

				RecalculateScheduledTimes();
			}

			this.Invalidate();
		}

		public void RemoveFinishedTrackInManualMode()
		{
			try
			{
				if (_currentPlayingIndex < 0 || _items.Count == 0)
				{
					Log($"[PlaylistQueue] RemoveFinishedTrackInManualMode ignorato: currentPlayingIndex={_currentPlayingIndex}, items={_items.Count}");
					return;
				}

				if (_lastPlayedForReport != null && _currentPlayingIndex == 0 && _items.Count > 0)
				{
					try
					{
						ReportManager.LogTrack(
							_lastPlayedForReport.ItemType,
							_lastPlayedForReport.Artist,
							_lastPlayedForReport.Title,
							_lastPlayedForReport.ActualStartTime,
							DateTime.Now,
							_lastPlayedForReport.Duration
						);

						ReportUpdated?.Invoke(this, EventArgs.Empty);
					}
					catch (Exception ex)
					{
						Log($"[PlaylistQueue] Errore report: {ex.Message}");
					}
				}

				if (_items.Count > 0)
				{
					_items.RemoveAt(0);

					_currentPlayingIndex = -1;
					_lastPlayedForReport = null;

					RecalculateScheduledTimes();
					UpdateScrollBar();
					this.Invalidate();
					NotifyQueueCountChanged();
				}
			}
			catch (Exception ex)
			{
				Log($"[PlaylistQueue] Errore rimozione:  {ex.Message}");
			}
		}

		public void AddItem(PlaylistQueueItem item)
		{
			_items.Add(item);
			RecalculateScheduledTimes();
			UpdateScrollBar();
			this.Invalidate();
			NotifyQueueCountChanged();
		}

		private void AddItemBatch(PlaylistQueueItem item)
		{
			_items.Add(item);
		}

		private void InsertItemBatch(int index, PlaylistQueueItem item)
		{
			if (index >= 0 && index <= _items.Count)
				_items.Insert(index, item);
		}

		private void FinalizeBatchModification()
		{
			RecalculateScheduledTimes();
			UpdateScrollBar();
			this.Invalidate();
			NotifyQueueCountChanged();
		}

		public void InsertItem(int index, PlaylistQueueItem item)
		{
			if (index >= 0 && index <= _items.Count)
			{
				_items.Insert(index, item);
				RecalculateScheduledTimes();
				UpdateScrollBar();
				this.Invalidate();
				NotifyQueueCountChanged();
			}
		}

		public void LoadPlaylistOnAir(AirPlaylist playlist, bool immediate)
		{
			if (playlist?.Items == null || playlist.Items.Count == 0)
				return;

			Log($"[LoadPlaylistOnAir] Caricamento playlist '{playlist.Name}' - immediate={immediate}, {playlist.Items.Count} elementi");

			if (immediate)
			{
				// Determina l'indice di inserimento: subito dopo l'elemento in play
				int insertIndex = _currentPlayingIndex >= 0 ? _currentPlayingIndex + 1 : 0;

				// Rimuovi dalla queue tutti gli elementi NON in play, NON schedulati e NON ADV
				for (int i = _items.Count - 1; i >= 0; i--)
				{
					if (i == _currentPlayingIndex)
						continue;

					var item = _items[i];
					bool isProtected = item.IsScheduled || item.Type == PlaylistItemType.ADV;

					if (!isProtected)
					{
						_items.RemoveAt(i);
						if (i < _currentPlayingIndex)
							_currentPlayingIndex--;
						if (i < insertIndex)
							insertIndex = Math.Max(insertIndex - 1, _currentPlayingIndex >= 0 ? _currentPlayingIndex + 1 : 0);
					}
				}

				// Ricalcola insertIndex: inizia dall'elemento dopo quello in play, poi salta tutti gli scheduled/ADV consecutivi
				int startScan = _currentPlayingIndex >= 0 ? _currentPlayingIndex + 1 : 0;
				insertIndex = startScan;
				for (int i = startScan; i < _items.Count; i++)
				{
					if (_items[i].IsScheduled || _items[i].Type == PlaylistItemType.ADV)
						insertIndex = i + 1;
					else
						break;
				}

				// Log diagnostico per identificare la posizione di inserimento
				Log($"[LoadPlaylistOnAir] _currentPlayingIndex={_currentPlayingIndex}, elementi rimasti dopo pulizia={_items.Count}, insertIndex={insertIndex}");
				for (int i = 0; i < _items.Count; i++)
				{
					var dbgItem = _items[i];
					Log($"[LoadPlaylistOnAir]   [{i}] Type={dbgItem.Type}, IsScheduled={dbgItem.IsScheduled}, Title='{dbgItem.Title}'");
				}

				// Inserisci gli elementi della playlist a partire da insertIndex
				int currentInsert = insertIndex;
				foreach (var pItem in playlist.Items)
				{
					var queueItem = ConvertPlaylistItem(pItem);
					if (queueItem != null)
					{
						queueItem.IsFromPlaylist = true;
						_items.Insert(currentInsert, queueItem);
						currentInsert++;
					}
				}
			}
			else
			{
				// Accoda: aggiunge tutti gli elementi in fondo
				foreach (var pItem in playlist.Items)
				{
					var queueItem = ConvertPlaylistItem(pItem);
					if (queueItem != null)
					{
						queueItem.IsFromPlaylist = true;
						_items.Add(queueItem);
					}
				}
			}

			RecalculateScheduledTimes();
			UpdateScrollBar();
			this.Invalidate();
			NotifyQueueCountChanged();

			Log($"[LoadPlaylistOnAir] Completato. Elementi in coda: {_items.Count}");
		}

		private PlaylistQueueItem ConvertPlaylistItem(AirPlaylistItem pItem)
		{
			try
			{
				switch (pItem.Type)
				{
					case AirPlaylistItemType.Track:
					{
						var allMusic = DbcManager.LoadFromCsv<MusicEntry>("Music.dbc");
						var entry = allMusic.FirstOrDefault(m =>
							string.Equals(m.FilePath, pItem.FilePath, StringComparison.OrdinalIgnoreCase));
						if (entry != null)
							return CreateMusicQueueItem(entry);

						Log($"[ConvertPlaylistItem] Track non trovata nel DB: {pItem.FilePath}");
						return null;
					}

					case AirPlaylistItemType.Clip:
					{
						// Check if this is a Clips RULE (no direct file, has rule metadata)
						if (string.IsNullOrEmpty(pItem.FilePath) && pItem.RuleSourceType == "Clips")
						{
							string catName = pItem.RuleCategoryName;
							string genName = pItem.RuleGenreName;
							bool hasCat = !string.IsNullOrEmpty(catName);
							bool hasGen = !string.IsNullOrEmpty(genName);

							if (hasCat && hasGen)
							{
								string combined = catName + " + " + genName;
								return GetRandomClipByCategoryAndGenre(combined);
							}
							else if (hasGen)
							{
								return GetRandomClipByGenre(genName);
							}
							else if (hasCat)
							{
								return GetRandomItemByCategory(catName, "Clip", false, 0, 0);
							}

							Log($"[ConvertPlaylistItem] Clip rule senza filtri validi");
							return null;
						}

						// Direct clip file
						var allClips = DbcManager.LoadFromCsv<ClipEntry>("Clips.dbc");
						var entry = allClips.FirstOrDefault(c =>
							string.Equals(c.FilePath, pItem.FilePath, StringComparison.OrdinalIgnoreCase));
						if (entry != null)
							return CreateClipQueueItem(entry);

						Log($"[ConvertPlaylistItem] Clip non trovata nel DB: {pItem.FilePath}");
						return null;
					}

					case AirPlaylistItemType.Category:
					{
						string categoryName = pItem.CategoryName ?? "";
						bool isClipsRule = pItem.RuleSourceType == "Clips";

						if (categoryName.Contains(" / "))
						{
							// Regola combinata Categoria+Genere
							string combined = categoryName.Replace(" / ", " + ");
							if (isClipsRule)
								return GetRandomClipByCategoryAndGenre(combined);
							return GetRandomMusicByCategoryAndGenre(combined, pItem.YearFilterEnabled, pItem.YearFrom, pItem.YearTo);
						}

						return GetRandomItemByCategory(categoryName, isClipsRule ? "Clip" : "Music", pItem.YearFilterEnabled, pItem.YearFrom, pItem.YearTo);
					}

					case AirPlaylistItemType.Genre:
					{
						string genreName = pItem.CategoryName ?? "";
						bool isClipsRule = pItem.RuleSourceType == "Clips";

						if (isClipsRule)
							return GetRandomClipByGenre(genreName);
						return GetRandomMusicByGenre(genreName, pItem.YearFilterEnabled, pItem.YearFrom, pItem.YearTo);
					}

					default:
						Log($"[ConvertPlaylistItem] Tipo non gestito: {pItem.Type}");
						return null;
				}
			}
			catch (Exception ex)
			{
				Log($"[ConvertPlaylistItem] Errore conversione item '{pItem.Title}': {ex.Message}");
				return null;
			}
		}


		public void RemoveItem(int index)
		{
			if (index >= 0 && index < _items.Count)
			{
				_items.RemoveAt(index);

				if (index == _currentPlayingIndex)
					_currentPlayingIndex = -1;
				else if (index < _currentPlayingIndex)
					_currentPlayingIndex--;

				RecalculateScheduledTimes();
				UpdateScrollBar();
				this.Invalidate();
				NotifyQueueCountChanged();
			}
		}

		public void Clear()
		{
			if (_currentPlayingIndex == 0 && _items.Count > 0)
			{
				var playingItem = _items[0];
				_items.Clear();
				_items.Add(playingItem);
				_currentPlayingIndex = 0;
			}
			else
			{
				_items.Clear();
				_currentPlayingIndex = -1;
			}

			_scrollOffset = 0;
			UpdateScrollBar();
			this.Invalidate();
			NotifyQueueCountChanged();
		}

		public int GetItemCount()
		{
			return _items.Count;
		}

		public List<PlaylistQueueItem> GetAllItems()
		{
			try { return new List<PlaylistQueueItem>(_items); }
			catch (InvalidOperationException) { return new List<PlaylistQueueItem>(); }
			catch (ArgumentException) { return new List<PlaylistQueueItem>(); }
		}

		public PlaylistQueueItem GetCurrentPlayingItem()
		{
			if (_currentPlayingIndex >= 0 && _currentPlayingIndex < _items.Count)
				return _items[_currentPlayingIndex];
			return null;
		}

		public PlaylistQueueItem GetNextItem()
		{
			int nextIndex = _currentPlayingIndex + 1;
			if (nextIndex >= 0 && nextIndex < _items.Count)
				return _items[nextIndex];
			return null;
		}

		public PlaylistQueueItem GetLastPlayedItem()
		{
			int lastIndex = _currentPlayingIndex - 1;
			if (lastIndex >= 0 && lastIndex < _items.Count)
				return _items[lastIndex];
			return null;
		}

		public (ScheduleEntry schedule, TimeSpan remaining)? GetNextSchedule()
		{
			try
			{
				DateTime now = DateTime.Now;
				int currentDayOfWeek = (int)now.DayOfWeek;

				var schedules = DbcManager.LoadFromCsv<ScheduleEntry>("Schedules.dbc");
				var activeSchedules = schedules.Where(s =>
					s.IsEnabled == 1 &&
					IsDayEnabled(s, currentDayOfWeek)).ToList();

				ScheduleEntry nextSchedule = null;
				DateTime nextScheduleTime = DateTime.MaxValue;

				foreach (var schedule in activeSchedules)
				{
					var times = schedule.Times.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

					foreach (var timeStr in times)
					{
						if (TimeSpan.TryParse(timeStr, out TimeSpan scheduleTime))
						{
							DateTime scheduleDateTime = now.Date.Add(scheduleTime);

							if (scheduleDateTime > now && scheduleDateTime < nextScheduleTime)
							{
								nextSchedule = schedule;
								nextScheduleTime = scheduleDateTime;
							}
						}
					}
				}

				if (nextSchedule != null)
				{
					TimeSpan remaining = nextScheduleTime - now;
					return (nextSchedule, remaining);
				}

				return null;
			}
			catch
			{
				return null;
			}
		}

        private void NotifyQueueCountChanged()
        {
            try
            {
                QueueCountChanged?.Invoke(this, _items.Count);
            }
            catch (Exception ex)
            {
                Log($"[NotifyQueueCountChanged] Errore: {ex.Message}");
            }
        }

		private void Log(string m) { _dailyLogger?.Log(m); }
		private void LogErr(string m, Exception ex) { _dailyLogger?.LogErr(m, ex); }
		private void LogErr(string m) { _dailyLogger?.LogErr(m); }

        protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_queueMonitorTimer?.Stop();
				_queueMonitorTimer?.Dispose();
				_scheduleMonitorTimer?.Stop();
				_scheduleMonitorTimer?.Dispose();
				try { _dailyLogger?.Dispose(); } catch { }
				_buttonToolTip?.Dispose();
			}
			base.Dispose(disposing);
		}
	}

	public enum PlaylistItemType
	{
		Music,
		Clip,
		ADV,
		Other
	}

	public class PlaylistQueueItem
	{
		public PlaylistItemType Type { get; set; }
		public DateTime ScheduledTime { get; set; }
		public DateTime ActualStartTime { get; set; }
		public string Artist { get; set; }
		public string Title { get; set; }
		public int Year { get; set; }
		public TimeSpan Duration { get; set; }
		public TimeSpan Intro { get; set; }
		public string FilePath { get; set; }

		public int MarkerIN { get; set; }
		public int MarkerINTRO { get; set; }
		public int MarkerMIX { get; set; }
		public int MarkerOUT { get; set; }

		/// <summary>Full file duration in milliseconds (before markers, actual file length)</summary>
		public int FileDurationMs { get; set; }

		public bool IsScheduled { get; set; }
		public bool IsFromPlaylist { get; set; }
		public MusicEntry OriginalMusicEntry { get; set; }

		public int ADVSpotCount { get; set; }
		public int ADVFileCount { get; set; }
        public string VideoFilePath { get; set; }
        public string VideoSource { get; set; }      // "StaticVideo", "BufferVideo", "NDISource"
        public string NDISourceName { get; set; }

        public string ItemType
		{
			get
			{
				if (Type == PlaylistItemType.ADV)
					return "ADV";

				if (!string.IsNullOrEmpty(FilePath) &&
					(FilePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
					 FilePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
				{
					return "Stream";
				}

				return Type == PlaylistItemType.Music ? "Music" : "Clip";
			}
		}

		public PlaylistQueueItem()
		{
			Type = PlaylistItemType.Other;
			ScheduledTime = DateTime.Now;
			ActualStartTime = DateTime.Now;
			Artist = string.Empty;
			Title = string.Empty;
			Year = 0;
			Duration = TimeSpan.Zero;
			Intro = TimeSpan.Zero;
			FilePath = string.Empty;

			MarkerIN = 0;
			MarkerINTRO = 0;
			MarkerMIX = 0;
			MarkerOUT = 0;

			IsScheduled = false;
			IsFromPlaylist = false;
			OriginalMusicEntry = null;
			ADVSpotCount = 0;
			ADVFileCount = 0;


            VideoFilePath = string.Empty;
            VideoSource = string.Empty;
            NDISourceName = string.Empty;
        }
	}

	public class AirDirectorPlaylistItem
	{
		public int ID { get; set; }
		public DateTime Date { get; set; }
		public string SlotTime { get; set; }
		public int SequenceOrder { get; set; }
		public string FileType { get; set; }
		public string FilePath { get; set; }
		public int Duration { get; set; }
		public string ClientName { get; set; }
		public string SpotTitle { get; set; }
		public string CampaignName { get; set; }
		public string CategoryName { get; set; }
		public bool IsActive { get; set; }

		public AirDirectorPlaylistItem()
		{
			Date = DateTime.Now;
			SlotTime = "";
			FileType = "";
			FilePath = "";
			ClientName = "";
			SpotTitle = "";
			CampaignName = "";
			CategoryName = "";
			IsActive = true;
		}
	}
}

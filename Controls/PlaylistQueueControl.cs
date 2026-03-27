using AirDirector.Forms;
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
		private const int ITEM_MARGIN = 2;

		private int _dragSourceIndex = -1;
		private bool _isDragging = false;
		private Point _dragStartPoint;

		private System.Windows.Forms.Timer _queueMonitorTimer;
		private System.Windows.Forms.Timer _scheduleMonitorTimer;
		private DateTime _nextDayScheduleLoadTime;

		private bool _isInitialStartup = true;
		private string _currentClockName = "";

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
		public event EventHandler<string> PreviewRequested;
		public event EventHandler<string> ClockChanged;
		public event EventHandler ReportUpdated;

		public PlaylistQueueControl()
		{
			InitializeComponent();
			_items = new List<PlaylistQueueItem>();

			this.DoubleBuffered = true;
			this.BackColor = Color.Black;

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

			_queueMonitorTimer = new System.Windows.Forms.Timer { Interval = 5000 };
			_queueMonitorTimer.Tick += QueueMonitorTimer_Tick;
			_queueMonitorTimer.Start();

			_scheduleMonitorTimer = new System.Windows.Forms.Timer { Interval = 60000 };
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

				if (shouldReload)
				{
					Log($"[PlaylistADV] 🔄 Ricarica palinsesto: {reloadReason}");
					LoadAdvCache();
				}

				if (now >= _nextDayScheduleLoadTime && now < _nextDayScheduleLoadTime.AddMinutes(2))
				{
					LoadNextDaySchedules();
					CalculateNextDayScheduleLoadTime();
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

		// Cerca schedulazioni ADV nella finestra [now, now+60s] e le attiva subito o con timer interno.
		private void LookAheadAndExecuteADVSchedules(DateTime now)
		{
			try
			{
				if (_cachedAdvItems.Count == 0 || _advCacheDate != now.Date)
					LoadAdvCache();

				if (_cachedAdvItems.Count == 0) return;

				var todaySlots = _cachedAdvItems
					.Where(a => a.Date.Date == now.Date && a.IsActive)
					.GroupBy(a => a.SlotTime)
					.Select(g => new { SlotTime = g.Key, Items = g.OrderBy(x => x.SequenceOrder).ToList() })
					.ToList();

				TimeSpan windowStart = now.TimeOfDay;
				TimeSpan windowEnd = now.TimeOfDay.Add(TimeSpan.FromSeconds(60));

				foreach (var slot in todaySlots)
				{
					if (!TimeSpan.TryParse(slot.SlotTime, out TimeSpan slotTime)) continue;

					bool inWindow = slotTime >= windowStart && slotTime < windowEnd;
					if (!inWindow) continue;

					string advKey = $"ADV_{slot.SlotTime}_{now:yyyy-MM-dd}";
					if (_executedSchedules.Contains(advKey)) continue;

					double delayMs = (slotTime - now.TimeOfDay).TotalMilliseconds;

					// Segna subito come pianificata per evitare doppie attivazioni
					_executedSchedules.Add(advKey);

					var capturedSlot = slot;

					if (delayMs <= 500)
					{
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
						var parts = lines[i].Split(',');
						if (parts.Length < 12) continue;

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
							CampaignName = parts[9],
							CategoryName = parts[10],
							IsActive = bool.Parse(parts[11])
						};

						_cachedAdvItems.Add(item);
					}
					catch (Exception ex)
					{
						Log($"[PlaylistADV] ⚠️ Riga {i} saltata: {ex.Message}");
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
                            int clockStartIndex = _items.Count;
                            GeneratePlaylistFromClock(schedule.ClockName, 1000);
                            if (!string.IsNullOrEmpty(_pendingScheduleVideoBufferPath))
                            {
                                for (int i = clockStartIndex; i < _items.Count; i++)
                                    ApplyScheduleVideoBuffer(_items[i]);
                            }
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
						Type = PlaylistItemType.Music,
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
			_currentClockName = newClockName;
			ClockChanged?.Invoke(this, newClockName);
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

        public void GeneratePlaylistFromClock(string clockName, int maxItems = 1000)
        {
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

                var clocks = DbcManager.LoadFromCsv<ClockEntry>("Clocks.dbc");
                var clock = clocks.FirstOrDefault(c => c.ClockName == clockName);

                if (clock == null)
                {
                    Log($"[GenerateClock] ❌ Clock '{clockName}' non trovato!");
                    return;
                }

                if (string.IsNullOrEmpty(clock.Items))
                {
                    Log($"[GenerateClock] ⚠️ Clock '{clockName}' vuoto!");
                    return;
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
                    return;
                }

                int addedCount = 0;
                int totalAttempts = 0;
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
                            AddItemBatch(queueItem);
                            addedCount++;
                            addedForThisItem++;
                        }
                        else
                        {
                            Log($"[GenerateClock]   ❌ Nessun elemento trovato dopo {maxAttemptsPerItem} tentativi");
                        }
                    }

                    Log($"[GenerateClock] ✅ Aggiunti {addedForThisItem}/{itemsToAdd} per questo elemento");
                }

                if (addedCount > 0)
                    FinalizeBatchModification();

                _currentClockName = clockName;
                DbcManager.SetConfigValue("LastUsedClock", clockName);
                ClockChanged?.Invoke(this, clockName);

                Log($"");
                Log($"[GenerateClock] ═══════════════════════════════════════════");
                Log($"[GenerateClock] ✅ GENERAZIONE COMPLETATA");
                Log($"[GenerateClock] Totale aggiunti: {addedCount}");
                Log($"[GenerateClock] Totale tentativi: {totalAttempts}");
                Log($"[GenerateClock] ═══════════════════════════════════════════");
                Log($"");

                if (_isInitialStartup && _items.Count >= 2)
                    QueueReady?.Invoke(this, _items.Count);
            }
            catch (Exception ex)
            {
                Log($"[GenerateClock] ❌ ERRORE CRITICO: {ex.Message}");
                Log($"[GenerateClock] StackTrace: {ex.StackTrace}");
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
                    m.Categories == category &&
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
                    c.Categories == category &&
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
					var filtered = allMusic.Where(m => m.Categories == category).ToList();

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
					var filtered = allClips.Where(c => c.Categories == category).ToList();

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
                Intro = TimeSpan.FromMilliseconds(entry.MarkerINTRO),
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
				Intro = TimeSpan.FromMilliseconds(entry.MarkerINTRO),
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

					if (index != 0 && index != _currentPlayingIndex && _items[index].Type != PlaylistItemType.ADV)
					{
						int previewButtonX = itemRect.Right - 60;
						Rectangle previewRect = new Rectangle(previewButtonX, itemRect.Bottom - 28, 24, 24);

						if (previewRect.Contains(e.Location))
						{
							var item = _items[index];
							if (File.Exists(item.FilePath))
							{
								PreviewRequested?.Invoke(this, item.FilePath);
							}
							else
							{
								MessageBox.Show(LanguageManager.GetString("Queue.FileNotFound", "File non trovato!"), LanguageManager.GetString("Common.Error", "Errore"), MessageBoxButtons.OK, MessageBoxIcon.Error);
							}
							return;
						}
					}

					if (index != 0 && index != _currentPlayingIndex)
					{
						int xButtonX = itemRect.Right - 30;
						Rectangle deleteRect = new Rectangle(xButtonX, itemRect.Bottom - 28, 24, 24);

						if (deleteRect.Contains(e.Location))
						{
							RemoveItem(index);
							return;
						}
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
                if (e.Button == MouseButtons.Left && _dragSourceIndex >= 0 && !_isDragging)
                {
                    if (Math.Abs(e.X - _dragStartPoint.X) > 5 || Math.Abs(e.Y - _dragStartPoint.Y) > 5)
                    {
                        Log($"[MouseMove] ??  Inizio drag da index {_dragSourceIndex}");
                        Log($"[MouseMove] Item: '{_items[_dragSourceIndex].Title}'");

                        _isDragging = true;

                        // Cambia cursore
                        this.Cursor = Cursors.Hand;

                        // Avvia DragDrop
                        var result = this.DoDragDrop(_items[_dragSourceIndex], DragDropEffects.Move);

                        Log($"[MouseMove] ?? DragDrop completato, result: {result}");

                        // Ripristina cursore
                        this.Cursor = Cursors.Default;
                    }
                }
                else if (e.Button != MouseButtons.Left)
                {
                    // Ripristina cursore se non si sta draggando
                    this.Cursor = Cursors.Default;
                }
            }
            catch (Exception ex)
            {
                Log($"[MouseMove] ?? Errore: {ex.Message}");
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

		private void DrawPlaylistItem(Graphics g, PlaylistQueueItem item, int yPos, int index, bool isPlaying)
		{
			int width = this.Width - (_scrollBar.Visible ? _scrollBar.Width : 0) - 10;
			Rectangle rect = new Rectangle(5, yPos, width, ITEM_HEIGHT);

			Color bgColor;
			Color textColor;

			if (item.Type == PlaylistItemType.ADV)
			{
				bgColor = Color.FromArgb(200, 0, 0);
				textColor = Color.White;
			}
			else if (isPlaying)
			{
				bgColor = Color.Black;
				textColor = Color.White;
			}
			else if (item.IsScheduled)
			{
				bgColor = Color.FromArgb(138, 43, 226);
				textColor = Color.White;
			}
			else
			{
				switch (item.Type)
				{
					case PlaylistItemType.Music:
						bgColor = Color.FromArgb(255, 255, 100);
						textColor = Color.Black;
						break;
					case PlaylistItemType.Clip:
						bgColor = Color.FromArgb(100, 200, 255);
						textColor = Color.Black;
						break;
					default:
						bgColor = Color.FromArgb(30, 30, 30);
						textColor = Color.White;
						break;
				}
			}

			using (LinearGradientBrush brush = new LinearGradientBrush(
				rect,
				bgColor,
				Color.FromArgb(Math.Max(0, bgColor.R - 20), Math.Max(0, bgColor.G - 20), Math.Max(0, bgColor.B - 20)),
				LinearGradientMode.Vertical))
			{
				g.FillRectangle(brush, rect);
			}

			using (Pen pen = new Pen(Color.Gray, 1))
			{
				g.DrawRectangle(pen, rect);
			}

			int xPos = rect.X + 5;

			using (Font numFont = new Font("Segoe UI", 16, FontStyle.Bold))
			using (SolidBrush numBrush = new SolidBrush(textColor))
			{
				string numText = $"#{index + 1}";
				g.DrawString(numText, numFont, numBrush, xPos, rect.Y + 28);
				xPos += 45;
			}

			string icon;
			if (item.Type == PlaylistItemType.ADV)
			{
				icon = "💲";
			}
			else if (!string.IsNullOrEmpty(item.FilePath) &&
					 (item.FilePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
					  item.FilePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
			{
				icon = "🌐";
			}
			else if (item.Type == PlaylistItemType.Music)
			{
				icon = "🎵";
			}
			else
			{
				icon = "⚡";
			}

			using (Font iconFont = new Font("Segoe UI Emoji", 22, FontStyle.Regular))
			using (SolidBrush iconBrush = new SolidBrush(textColor))
			{
				g.DrawString(icon, iconFont, iconBrush, xPos, rect.Y + 24);
				xPos += 40;
			}

			xPos += 15;
			using (Font titleFont = new Font("Segoe UI", 13, FontStyle.Bold))
			using (SolidBrush titleBrush = new SolidBrush(textColor))
			{
				string titleText = string.IsNullOrEmpty(item.Artist)
					? item.Title
					: $"{item.Artist} - {item.Title}";

				if (item.Year > 0)
					titleText += $" ({item.Year})";

				g.DrawString(titleText, titleFont, titleBrush, xPos, rect.Y + 12);
			}

			if (item.Type != PlaylistItemType.ADV)
			{
				using (Font introFont = new Font("Segoe UI", 9, FontStyle.Regular))
				using (SolidBrush introBrush = new SolidBrush(textColor))
				{
					string introText = $"INTRO: {item.Intro.TotalSeconds:F1}s";
					SizeF introSize = g.MeasureString(introText, introFont);
					g.DrawString(introText, introFont, introBrush, rect.Right - introSize.Width - 10, rect.Y + 8);
				}
			}

			using (Font durationFont = new Font("Segoe UI", 10, FontStyle.Bold))
			using (SolidBrush durationBrush = new SolidBrush(textColor))
			{
				string durationText;
				if (item.Duration.TotalMinutes >= 60)
				{
					durationText = $"⏱ {item.Duration:hh\\:mm\\:ss}";
				}
				else
				{
					durationText = $"⏱ {item.Duration:mm\\:ss}";
				}

				SizeF durationSize = g.MeasureString(durationText, durationFont);
				g.DrawString(durationText, durationFont, durationBrush, rect.Right - durationSize.Width - 10, rect.Y + 26);
			}

			using (Font timeFont = new Font("Segoe UI", 8, FontStyle.Regular))
			using (SolidBrush timeBrush = new SolidBrush(textColor))
			{
				string timeText = item.ScheduledTime.ToString("HH:mm:ss");

				if (item.Type == PlaylistItemType.Music && item.OriginalMusicEntry != null)
				{
					var violations = GetViolations(item.OriginalMusicEntry);
					if (violations.Count > 0)
						timeText += " | ⚠ " + string.Join(", ", violations);
				}

				g.DrawString(timeText, timeFont, timeBrush, xPos, rect.Bottom - 20);
			}

			if (item.IsScheduled)
			{
				using (Font schedFont = new Font("Segoe UI", 7, FontStyle.Bold))
				using (SolidBrush schedBrush = new SolidBrush(Color.Yellow))
				{
					string schedText = "SCHEDULED";
					SizeF schedSize = g.MeasureString(schedText, schedFont);
					g.DrawString(schedText, schedFont, schedBrush, rect.Right - schedSize.Width - 90, rect.Bottom - 20);
				}
			}

			if (index != 0 && index != _currentPlayingIndex && item.Type != PlaylistItemType.ADV)
			{
				int previewButtonX = rect.Right - 60;
				Rectangle previewRect = new Rectangle(previewButtonX, rect.Bottom - 28, 24, 24);

				using (SolidBrush previewBrush = new SolidBrush(Color.FromArgb(200, 0, 120, 215)))
				{
					g.FillEllipse(previewBrush, previewRect);
				}

				using (Font headphoneFont = new Font("Segoe UI", 14, FontStyle.Bold))
				using (SolidBrush headphoneTextBrush = new SolidBrush(Color.White))
				{
					StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
					g.DrawString("▶", headphoneFont, headphoneTextBrush, previewRect, sf);
				}
			}

			if (index != 0 && index != _currentPlayingIndex)
			{
				int xButtonX = rect.Right - 30;
				Rectangle deleteRect = new Rectangle(xButtonX, rect.Bottom - 28, 24, 24);

				using (SolidBrush xBrush = new SolidBrush(Color.FromArgb(200, 255, 0, 0)))
				{
					g.FillEllipse(xBrush, deleteRect);
				}

				using (Font xFont = new Font("Segoe UI", 12, FontStyle.Bold))
				using (SolidBrush xTextBrush = new SolidBrush(Color.White))
				{
					StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
					g.DrawString("✖", xFont, xTextBrush, deleteRect, sf);
				}
			}
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
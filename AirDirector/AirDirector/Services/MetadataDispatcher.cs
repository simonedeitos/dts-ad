using System;

namespace AirDirector.Services
{
    /// <summary>
    /// Dispatcher statico per eventi metadata - evita reflection
    /// </summary>
    public static class MetadataDispatcher
    {
        public static event EventHandler<MetadataEventArgs> MetadataUpdated;

        public static void RaiseMetadataUpdate(string artist, string title)
        {
            MetadataUpdated?.Invoke(null, new MetadataEventArgs(artist, title));
        }
    }

    public class MetadataEventArgs : EventArgs
    {
        public string Artist { get; }
        public string Title { get; }

        public MetadataEventArgs(string artist, string title)
        {
            Artist = artist;
            Title = title;
        }
    }
}
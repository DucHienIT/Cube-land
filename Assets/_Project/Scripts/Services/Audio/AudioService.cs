namespace CubeBlaster
{
    public static class AudioService
    {
        static IAudioService _current;

        public static IAudioService Current => _current ?? NullAudioService.Instance;

        public static void Register(IAudioService service)
        {
            if (service != null) _current = service;
        }

        public static void Unregister(IAudioService service)
        {
            if (ReferenceEquals(_current, service)) _current = null;
        }
    }
}

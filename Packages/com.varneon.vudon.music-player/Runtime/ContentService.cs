using System;
using UnityEngine;

namespace Varneon.VUdon.MusicPlayer.Enums
{
    [Obsolete]
    public enum ContentService
    {
        [InspectorName("YouTube")]
        YouTube,
        VRCDN,
        Soundcloud,
        Twitch,
        Vimeo,
        Mixcloud,
        [InspectorName("NicoNico")]
        NicoNico,
        Youku,
        GoogleDrive,
        FacebookVideo,
        Other
    }
}

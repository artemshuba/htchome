using System.Collections.Generic;

namespace HTCHome.State
{
    sealed record AppState
    {
        /// <summary>
        /// Widget IDs of the currently loaded widgets.
        /// </summary>
        public List<string> Widgets { get; set; } = [];
    }
}

using UnityEngine;

namespace REIW
{
    public class PathData
    {
        [field: SerializeField, ReadOnly] public long PathEndMs { get; set; }
        [field: SerializeField, ReadOnly] public long WholeDurationMs { get; set; }
        [field: SerializeField, ReadOnly] public long MoveDurationMs { get; set; }
    }
}
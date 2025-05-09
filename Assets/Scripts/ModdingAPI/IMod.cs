using UnityEngine;

namespace DLS.ModdingAPI
{
    public abstract class IMod
    {
        public abstract string ModID { get; }
        public abstract string Name { get; }
        public abstract string Version { get; }
        public abstract void Initialize();

        // Optional event methods with default implementations
        public virtual void OnPlaceChip(ChipEventArgs chip) { }
        public virtual void OnMoveChip(ChipEventArgs chip, Vector2 newPosition) { }
        public virtual void OnPlaceWire(WireEventArgs wire) { }
        public virtual void OnEditWire(WireEventArgs wire) { }
        public virtual void OnProjectLoad(ProjectEventArgs project) { }
        public virtual void OnProjectUnload(ProjectEventArgs project) { }
        public virtual void OnMouseClick(InputEventArgs args) { }

        public enum MouseButton
        {
            Left = 0,
            Right = 1,
            Middle = 2
        }

        public struct ChipEventArgs
        {
            public string ChipName;
            public Vector2 Position;
        }

        public struct WireEventArgs
        {
            public string SourcePinName;
            public string TargetPinName;
            public int BitCount;
        }

        public struct ProjectEventArgs
        {
            public string ProjectName;
            public string FilePath;
        }

        public struct InputEventArgs
        {
            public Vector2 Position;
            public MouseButton Button;
        }
    }
}
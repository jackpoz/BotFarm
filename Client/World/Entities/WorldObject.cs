using Client.World.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.World.Entities
{
    public class WorldObject : Position
    {
        public ulong GUID
        {
            get
            {
                return _guid;
            }
            set
            {
                Reset();
                _guid = value;
            }
        }
        ulong _guid;

        public uint this[PlayerField index]
        {
            get
            {
                return this[(int)index];
            }
            set
            {
                this[(int)index] = value;
            }
        }

        public uint this[UnitField index]
        {
            get
            {
                return this[(int)index];
            }
            set
            {
                this[(int)index] = value;
            }
        }

        public uint this[int index]
        {
            get
            {
                uint value;
                objectFields.TryGetValue(index, out value);
                return value;
            }
            set
            {
                objectFields[index] = value;
                if (OnFieldUpdated != null)
                    OnFieldUpdated(this, new UpdateFieldEventArg(index, value, this));
            }
        }
        Dictionary<int, uint> objectFields = new Dictionary<int, uint>();

        public event EventHandler<UpdateFieldEventArg> OnFieldUpdated;

        protected virtual void Reset()
        {
            objectFields.Clear();
        }

        public bool IsType(HighGuid highGuidType)
        {
            return Utility.IsType(GUID, highGuidType);
        }
    }

    public class UpdateFieldEventArg : EventArgs
    {
        public int Index
        {
            get;
            private set;
        }

        public uint NewValue
        {
            get;
            private set;
        }

        public WorldObject Object
        {
            get;
            private set;
        }

        public UpdateFieldEventArg(int Index, uint NewValue, WorldObject Object)
        {
            this.Index = Index;
            this.NewValue = NewValue;
            this.Object = Object;
        }
    }

    public enum HighGuid
    {
        Player = 0x000,
        BattleGround1 = 0x101,
        InstanceSave = 0x104,
        Group = 0x105,
        BattleGround2 = 0x109,
        MOTransport = 0x10C,
        Guild = 0x10F,
        Item = 0x400,
        DynObject = 0xF00,
        GameObject = 0xF01,
        Transport = 0xF02,
        Unit = 0xF03,
        Pet = 0xF04,
        Vehicle = 0xF05
    }
}

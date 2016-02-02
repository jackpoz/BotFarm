using Client.World;
using Client.World.Entities;
using Client.World.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class Trigger
    {
        public int Id
        {
            get;
            set;
        }

        public List<TriggerAction> TriggerActions
        {
            get;
            private set;
        }

        public Action FinalAction
        {
            get;
            private set;
        }

        public Trigger(IEnumerable<TriggerAction> TriggerActions, Action FinalAction)
        {
            this.TriggerActions = TriggerActions.ToList();
            this.FinalAction = FinalAction;
        }

        public void HandleInput(TriggerActionType type, params object[] inputs)
        {
            if (TriggerActions.Last().Triggered)
                return;

            foreach (var trigger in TriggerActions)
            {
                if (trigger.Triggered)
                    continue;

                trigger.HandleInput(type, inputs);
                break;
            }

            if (TriggerActions.Last().Triggered)
            {
                if (FinalAction != null)
                    FinalAction();
                Reset();
            }
        }

        public void Reset()
        {
            TriggerActions.ForEach(trigger => trigger.Reset());
        }
    }

    public class OpcodeTriggerAction : TriggerAction
    {
        public WorldCommand Opcode
        {
            get;
            private set;
        }

        public Func<Packet, bool> PacketChecker
        {
            get;
            private set;
        }

        public OpcodeTriggerAction(WorldCommand Opcode, Action IntermediateAction = null)
            : base(TriggerActionType.Opcode, IntermediateAction)
        {
            this.Opcode = Opcode;
        }

        public OpcodeTriggerAction(WorldCommand Opcode, Func<Packet, bool> PacketChecker, Action IntermediateAction = null)
            : base(TriggerActionType.Opcode, IntermediateAction)
        {
            this.Opcode = Opcode;
            this.PacketChecker = PacketChecker;
        }

        protected override bool CheckIfTriggered(TriggerActionType type, params object[] inputs)
        {
            if (inputs == null || inputs.Length == 0)
                return false;

            var packet = inputs[0] as Packet;
            if (packet == null)
                return false;

            if (packet.Header.Command != Opcode)
                return false;

            if (PacketChecker != null)
                return PacketChecker(packet);
            else
                return true;
        }
    }

    public class UpdateFieldTriggerAction : TriggerAction
    {
        public int Index
        {
            get;
            private set;
        }

        public uint Value
        {
            get;
            private set;
        }

        public UpdateFieldTriggerAction(int Index, uint Value,Action IntermediateAction = null)
            : base(TriggerActionType.UpdateField, IntermediateAction)
        {
            this.Index = Index;
            this.Value = Value;
        }

        protected override bool CheckIfTriggered(TriggerActionType type, params object[] inputs)
        {
            if (inputs == null || inputs.Length == 0 || inputs[0] == null || inputs[0].GetType() != typeof(UpdateFieldEventArg))
                return false;

            var updateFieldArgs = (UpdateFieldEventArg)inputs[0];
            if (updateFieldArgs.Index != Index)
                return false;

            if (updateFieldArgs.NewValue != Value)
                return false;

            return true;
        }
    }

    public class CustomTriggerAction : TriggerAction
    {
        public Func<object[], bool> Checker
        {
            get;
            private set;
        }

        public CustomTriggerAction(TriggerActionType Type, Func<object[], bool> Checker, Action IntermediateAction = null)
            : base(Type, IntermediateAction)
        {
            this.Checker = Checker;
        }

        protected override bool CheckIfTriggered(TriggerActionType type, params object[] inputs)
        {
            if (Checker == null)
                return false;

            return Checker(inputs);
        }
    }

    public class AlwaysTrueTriggerAction : TriggerAction
    {
        public AlwaysTrueTriggerAction(TriggerActionType Type, Action IntermediateAction = null)
            : base(Type, IntermediateAction)
        { }

        protected override bool CheckIfTriggered(TriggerActionType type, params object[] inputs)
        {
            return true;
        }
    }

    public abstract class TriggerAction
    {
        public TriggerActionType Type
        {
            get;
            private set;
        }

        public bool Triggered
        {
            get;
            set;
        }

        public Action IntermediateAction
        {
            get;
            private set;
        }

        public TriggerAction(TriggerActionType Type, Action IntermediateAction = null)
        {
            this.Type = Type;
            this.IntermediateAction = IntermediateAction;
        }

        public void Reset()
        {
            Triggered = false;
        }

        public void HandleInput(TriggerActionType type, params object[] inputs)
        {
            if (type != Type)
                return;

            if (CheckIfTriggered(type, inputs))
            {
                Triggered = true;
                if (IntermediateAction != null)
                    IntermediateAction();
            }
        }

        protected abstract bool CheckIfTriggered(TriggerActionType type, params object[] inputs);
    }

    public enum TriggerActionType
    {
        None,
        Opcode,
        UpdateField,
        DestinationReached,
        TradeCompleted
    }
}

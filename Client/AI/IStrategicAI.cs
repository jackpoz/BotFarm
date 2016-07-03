using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.AI
{
    public interface IStrategicAI : IGameAI
    {
    }


    public class EmptyStrategicAI : IStrategicAI
    {
        public bool Activate(AutomatedGame game)
        {
            return true;
        }

        public void Deactivate()
        { }

        public void Pause()
        { }

        public void Resume()
        { }

        public void Update()
        { }

        public bool AllowPause()
        {
            return true;
        }
    }
}

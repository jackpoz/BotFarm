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
        {
            // Default empty implementation
        }

        public void Pause()
        {
            // Default empty implementation
        }

        public void Resume()
        {
            // Default empty implementation
        }

        public void Update()
        {
            // Default empty implementation
        }

        public bool AllowPause()
        {
            return true;
        }
    }
}

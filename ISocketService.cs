using System;
using System.Collections.Generic;
using System.Text;

namespace CoreliumSocketAPI
{
    public interface ISocketService
    {
        void Stop();
        bool IsConnected();
        ISocketService Start();
        CoreliumSocket GetServer();
    }
}

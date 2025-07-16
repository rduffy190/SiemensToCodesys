using System;

namespace SiemensSCLToPlcOpen
{
    public enum BuildState
    {
        GetType,
        GetInterface,
        GetIn,
        GetOut,
        GetInOut, 
        GetVar,
        GetVarRetain, 
        GetVarTemp,
        GetCode, 
        Create
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServoModbus.Utils;

public static class ServoDataAdapter
{
    public static H17 GetH17(ushort[] data)
    {
        if (data.Length < 65)
        {
            throw new ArgumentException("H17长度不足");
        }
        H17 h17 = new H17();
        for (int i = 0; i < 16; i++)
        {
            h17.VDITable.VDIInfo[i].DIFuncType = (DIFuncType)data[2 * i];
            h17.VDITable.VDIInfo[i].High = data[2 * i + 1];
            h17.VDO[i] = (data[32] & (1 << i)) != 0;
            h17.VDOTable.VDOInfo[i].DOFuncType = (DOFuncType)data[2 * i + 33];
            h17.VDOTable.VDOInfo[i].High = data[2 * i + 34];
        }
        return h17;
    }
}

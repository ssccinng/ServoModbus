using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServoModbus;

public class VDITable
{
    // 还需获取实际状态 通过枚举获取
    public VDIInfo[] VDIInfo { get; set; } = new VDIInfo[64];
}

public class VDIInfo
{
    public DIFuncType DIFuncType { get; set; }
    /// <summary>
    /// 是否高电平有效
    /// </summary>
    public int High { get; set; }
}

public class VDOTable
{
    public VDOInfo[] VDOInfo { get; set; } = new VDOInfo[64];
}
public class VDOInfo
{
    public DOFuncType DOFuncType { get; set; }
    /// <summary>
    /// 是否高电平有效
    /// </summary>
    public int High { get; set; }

}
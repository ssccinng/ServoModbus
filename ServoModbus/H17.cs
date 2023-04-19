using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServoModbus;
/// <summary>
/// 虚拟DIDO设置
/// </summary>
public class H17
{
    public VDITable VDITable { get; set; } = new VDITable();
    public bool[] VDO { get; set; } = new bool[16];
    public VDOTable VDOTable { get; set; } = new VDOTable();
}

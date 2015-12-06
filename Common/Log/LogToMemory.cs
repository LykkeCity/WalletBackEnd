using System;
using System.Threading.Tasks;
using Common.RemoteUi;

namespace Common.Log
{
    public static class LogUtils
    {
        public static readonly string[] GuiHeader =
        {
            "Date Time", "Level", "Component", "Process", "Context", "Type", "Msg"
        };
    }

    public class LogToMemory : ILog, IGuiTable
    {
        private readonly GuiTableLastData _guiTableLastData = new GuiTableLastData(50, LogUtils.GuiHeader);

        private void WriteRecordToMemory(string level, string component, string process,
            string context, string type, string msg, DateTime? dateTime)
        {
            if (dateTime == null)
                dateTime = DateTime.UtcNow;
            _guiTableLastData.NewData(
                dateTime.Value.ToString(Utils.IsoDateTimeMask),
                level,
                component,
                process,
                context,
                type,
                msg);
        }

        public Task WriteInfo(string component, string process, string context, string info, DateTime? dateTime = null)
        {
            WriteRecordToMemory("info", component, process, context, string.Empty, info, dateTime);

            return Task.FromResult(0);
        }

        public Task WriteWarning(string component, string process, string context, string info, DateTime? dateTime = null)
        {
            WriteRecordToMemory("warning", component, process, context, string.Empty, info, dateTime);
            return Task.FromResult(0);
        }

        public Task WriteError(string component, string process, string context, Exception type, DateTime? dateTime = null)
        {
            WriteRecordToMemory("error", component, process, context, type.GetType().ToString(), type.Message, dateTime);
            return Task.FromResult(0);
        }

 
        public Task WriteFatalError(string component, string process, string context, Exception type, DateTime? dateTime = null)
        {
            WriteRecordToMemory("fatalerror", component, process, context, type.GetType().ToString(), type.Message, dateTime);
            return Task.FromResult(0);
        }

        public GuiTableData TableData { get { return _guiTableLastData.TableData; } }

        public int Count { get { return _guiTableLastData.Count; } }


        public void Clear()
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Diagnostics
{
    public static class DetailedLogger
    {
        private static class Config
        {
            public static long BufferSize;
            public static int WriteToFileDelay;
            public static Int16 MaxWritesInOneFile;
            public static int LinesPerTrace;

            private static readonly string[] AvailableSections = new string[] { "detailedLogger", "dmis/detailedLogger" };
            static Config()
            {
                NameValueCollection collection = null;
                for (int i = 0; i < AvailableSections.Length; i++)
                {
                    collection = ConfigurationManager.GetSection(AvailableSections[i]) as NameValueCollection;
                    if (collection != null)
                        break;
                }

                BufferSize = Parse<long>(collection, "BufferSize", 10000);
                WriteToFileDelay = Parse<int>(collection, "WriteToFileDelay", 1000);
                MaxWritesInOneFile = Parse<Int16>(collection, "MaxWritesInOneFile", 100);
                LinesPerTrace = Parse<int>(collection, "LinesPerTrace", 1000);
            }

            private static T Parse<T>(NameValueCollection collection, string key, T defaultValue)
            {
                if (collection == null)
                    return defaultValue;

                var value = collection.Get(key);
                if (String.IsNullOrEmpty(value))
                    return defaultValue;

                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch (Exception e)
                {
                    throw new ApplicationException(String.Format("Invalid configuration: key: '{0}', value: '{1}'.", key, value), e);
                }
            }

        }

        public class Operation
        {
            private static long _nextId = 1;

            internal static Operation Null = new Operation(0L);

            public string Msg { get; internal set; }
            public DateTime StartedAt { get; internal set; }
            public long Id { get; private set; }

            private Operation(long id)
            {
                Id = id;
            }
            public Operation()
            {
                //Id = Guid.NewGuid();
                Id = Interlocked.Increment(ref _nextId) - 1;
            }

            [Conditional("DIAGNOSTIC")]
            public void Finish()
            {
                //_finishAction = () => WriteEndToLog(this, true, null, null);
                Finish(null);
            }
            [Conditional("DIAGNOSTIC")]
            public void Finish(string message, params object[] args)
            {
                //_finishAction = () => WriteEndToLog(this, true, message, args);
                WriteEndToLog(this, true, message, args);
            }

            //private Action _finishAction;

            //public void Dispose()
            //{
            //    if(_finishAction == null)
            //        WriteEndToLog(this, false, null, null);
            //    _finishAction.Invoke();
            //}
        }


        public static Operation CreateOperation()
        {
#if DIAGNOSTIC
            return new Operation();
#else
            return Operation.Null;
#endif
        }

        private static readonly object[] _emptyArgs = new string[0];
        private static void WriteEndToLog(Operation op, bool successful, string message, object[] args)
        {
            if (message == null)
            {
                message = op.Msg;
                args = _emptyArgs;
            }
            if (args == null)
                args = _emptyArgs;

            // protection against unprintable characters
            var line = FinishOperation(op, successful, message, args);

            // writing to the buffer
            var p = Interlocked.Increment(ref _bufferPosition) - 1;
            _buffer[p % Config.BufferSize] = line;

        }

        private static string[] _buffer = new string[Config.BufferSize];

        private static long _bufferPosition = 0; // this field is incremented by every logger thread.
        private static long _lastBufferPosition = 0; // this field is written by only CollectLines method.

        /// <summary>Statistical data: the longest gap between p0 and p1</summary>
        private static long _maxPdiff = 0;

        /*================================================================== Logger */

#if DEBUG
        private static long _maxLogTicks;
        private static long _maxLogTicksPeak;
        private static long _maxWriteTicks;
        private static long _maxWriteTicksPeak;
        private static bool _warmup = true;
#endif

        [Conditional("DIAGNOSTIC")]
        public static void Log(string message, params object[] args)
        {
#if DEBUG
            var t = Stopwatch.StartNew();
#endif
            // protection against unprintable characters
            var line = SafeFormatString(null, message, args);

            // writing to the buffer
            var p = Interlocked.Increment(ref _bufferPosition) - 1;
            _buffer[p % Config.BufferSize] = line;

#if DEBUG
            // measuring
            t.Stop();
            var tmax = Interlocked.Read(ref _maxLogTicks);
            var tpeak = Interlocked.Read(ref _maxLogTicksPeak);
            if (t.ElapsedTicks > tmax)
                Interlocked.Exchange(ref _maxLogTicks, t.ElapsedTicks);
            if (t.ElapsedTicks > tpeak)
                Interlocked.Exchange(ref _maxLogTicksPeak, t.ElapsedTicks);
#endif
        }
        [Conditional("DIAGNOSTIC")]
        public static void Log(Operation op, string message, params object[] args)
        {
#if DEBUG
            var t = Stopwatch.StartNew();
#endif
            // protection against unprintable characters
            var line = SafeFormatString(op, message, args);

            // writing to the buffer
            var p = Interlocked.Increment(ref _bufferPosition) - 1;
            _buffer[p % Config.BufferSize] = line;

#if DEBUG
            // measuring
            t.Stop();
            var tmax = Interlocked.Read(ref _maxLogTicks);
            var tpeak = Interlocked.Read(ref _maxLogTicksPeak);
            if (t.ElapsedTicks > tmax)
                Interlocked.Exchange(ref _maxLogTicks, t.ElapsedTicks);
            if (t.ElapsedTicks > tpeak)
                Interlocked.Exchange(ref _maxLogTicksPeak, t.ElapsedTicks);
#endif
        }

        private static string SafeFormatString(Operation op, string message, params object[] args)
        {
            var lineCounter = Interlocked.Increment(ref _lineCounter);
            var line = (op != null)
                ? String.Format("{0}\t{1}\tT:{2}\tOp:{3} START\t"
                    , lineCounter
                    , DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fffff")
                    , Thread.CurrentThread.ManagedThreadId
                    , op.Id)
                : String.Format("{0}\t{1}\tT:{2}\t"
                    , lineCounter
                    , DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fffff")
                    , Thread.CurrentThread.ManagedThreadId);

            string msg = null;
            try
            {
                var c = String.Format(message, args).ToCharArray();
                for (int i = 0; i < c.Length; i++)
                    if (c[i] < ' ' && c[i] != '\t')
                        c[i] = '.';
                msg = new string(c);
                line += msg;
            }
            catch (Exception e)
            {
                msg = String.Format("INVALID CALL: {0}. {1}", message, e);
                line = msg;
            }

            if (op != null)
            {
                op.Msg = msg;
                op.StartedAt = DateTime.UtcNow;
            }

            return line;
        }

        private static string FinishOperation(Operation op, bool successful, string message, params object[] args)
        {
            var lineCounter = Interlocked.Increment(ref _lineCounter);

            var line = String.Format("{0}\t{1}\tT:{2}\tOp:{3} END {4}({5})\t"
                , lineCounter
                , DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fffff")
                , Thread.CurrentThread.ManagedThreadId
                , op.Id
                , successful ? string.Empty : "UNTERMINATED "
                , DateTime.UtcNow - op.StartedAt);

            try
            {
                var c = (args == null || args.Length == 0) 
                    ? message.ToCharArray() 
                    : String.Format(message, args).ToCharArray();
                for (int i = 0; i < c.Length; i++)
                    if (c[i] < ' ' && c[i] != '\t')
                        c[i] = '.';
                line += new string(c);
            }
            catch (Exception e)
            {
                line = String.Format("INVALID CALL: {0}. {1}", message, e);
            }

            if (op != null)
            {
                op.Msg = line;
                op.StartedAt = DateTime.UtcNow;
            }

            return line;
        }

        /*================================================================== File writer */


        private static int _lineCounter;
        private static int _lastLineCounter;

        private static Timer _timer = new Timer(_ => WriteToFile(), null, Config.WriteToFileDelay, Config.WriteToFileDelay);

        private static object _writeSync = new object();
        private static void WriteToFile()
        {
            lock (_writeSync)
            {
#if DEBUG
            long twmax, twpeak;
            var t = Stopwatch.StartNew();
#endif

                _timer.Change(Timeout.Infinite, Timeout.Infinite); //stops the timer

                var text = CollectLines();
                if (text != null)
                {
                    using (var writer = new StreamWriter(LogFilePath, true))
                    {
                        writer.Write(text);
                        var lineCounter = _lineCounter;
                        if ((lineCounter - _lastLineCounter) > Config.LinesPerTrace)
                        {
#if DEBUG
                            if (_warmup)
                            {
                                Interlocked.Exchange(ref _maxLogTicks, 0);
                                Interlocked.Exchange(ref _maxWriteTicks, 0);
                                _warmup = false;
                            }
                            var tmax = Interlocked.Read(ref _maxLogTicks);
                            var tpeak = Interlocked.Exchange(ref _maxLogTicksPeak, 0);
                            twmax = Interlocked.Read(ref _maxWriteTicks);
                            twpeak = Interlocked.Exchange(ref _maxWriteTicksPeak, 0);

                            var msg = String.Format("MaxPdiff: {0}, MaxLog: {1}, PeakLog: {2}, MaxWrite: {3}, PeakWrite: {4}", _maxPdiff, tmax, tpeak, twmax, twpeak);
                            writer.WriteLine(msg);
                            Debug.WriteLine("#DETAILEDLOG> " + msg);
                            Console.WriteLine(msg);
#else
                            var msg = String.Format("MaxPdiff: {0}", _maxPdiff);
                            writer.WriteLine(msg);
                            Debug.WriteLine("#DETAILEDLOG> " + msg);
#endif
                            _lastLineCounter = lineCounter;
                        }
                    }
                }
                _timer.Change(Config.WriteToFileDelay, Config.WriteToFileDelay); //restart

#if DEBUG
            // measuring
            t.Stop();
            twmax = Interlocked.Read(ref _maxWriteTicks);
            twpeak = Interlocked.Read(ref _maxWriteTicksPeak);
            if (t.ElapsedTicks > twmax)
                Interlocked.Exchange(ref _maxWriteTicks, t.ElapsedTicks);
            if (t.ElapsedTicks > twpeak)
                Interlocked.Exchange(ref _maxWriteTicksPeak, t.ElapsedTicks);
#endif
            }
        }
        private static StringBuilder CollectLines()
        {
            var p0 = _lastBufferPosition;
            var p1 = Interlocked.Read(ref _bufferPosition);

            if (p0 == p1)
                return null;

            var sb = new StringBuilder(">"); // the '>' sign means: block writing start.
            var pdiff = p1 - p0;
            if (pdiff > _maxPdiff)
                _maxPdiff = pdiff;
            if (pdiff > Config.BufferSize)
            {
                sb.AppendFormat("BUFFER OVERRUN ERROR: Buffer size is {0}, unwritten lines : {1}", Config.BufferSize, pdiff).AppendLine();
                Debug.WriteLine("#DETAILEDLOG> BUFFER OVERRUN ERROR: Buffer size is {0}, unwritten lines : {1}", Config.BufferSize, pdiff);
                Console.WriteLine("BUFFER OVERRUN ERROR: Buffer size is {0}, unwritten lines : {1}", Config.BufferSize, pdiff);
            }

            //----

            while (p0 < p1)
            {
                var p = p0 % Config.BufferSize;
                var line = _buffer[p];
                sb.AppendLine(line);
                //_buffer[p] = null;
#if DEBUG
            //    Console.WriteLine(line);
#endif
                Debug.WriteLine("#DETAILEDLOG> " + line);
                p0++;
            }
            //----

            _lastBufferPosition = p1;

            return sb;
        }

        private static object _sync = new object();
        private static Int16 _lineCount;
        private static string _logFilePath;
        private static string LogFilePath
        {
            get
            {
                if (_logFilePath == null || _lineCount >= Config.MaxWritesInOneFile)
                {
                    lock (_sync)
                    {
                        if (_logFilePath == null || _lineCount >= Config.MaxWritesInOneFile)
                        {
                            var logFilePath = Path.Combine(LogDirectory, "detailedlog_" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "Z.log");
                            if (!File.Exists(logFilePath))
                                using (FileStream fs = new FileStream(logFilePath, FileMode.Create))
                                using (StreamWriter wr = new StreamWriter(fs))
                                    wr.WriteLine("----");
                            _lineCount = 0;
                            _logFilePath = logFilePath;
                        }
                    }
                }
                _lineCount++;
                return _logFilePath;
            }
        }

        private static string __logDirectory = null;
        private static string LogDirectory
        {
            get
            {
                if (__logDirectory == null)
                {
                    var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data\\DetailedLog");
                    if (!Directory.Exists(logDirectory))
                        Directory.CreateDirectory(logDirectory);
                    __logDirectory = logDirectory;
                }
                return __logDirectory;
            }
        }

        public static void Flush()
        {
            WriteToFile();
        }
    }
}

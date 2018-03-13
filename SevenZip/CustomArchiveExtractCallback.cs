using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SevenZip
{
    class CustomArchiveExtractCallback : CallbackBase, IDisposable, IArchiveExtractCallback
    {
        List<ArchiveFileInfo> _files;
        ExtractFileCallback _cb;

        public CustomArchiveExtractCallback(List<ArchiveFileInfo> files, ExtractFileCallback cb)
        {
            _files = files;
            _cb = cb;
        }

        public void Dispose()
        {
        }

        public void SetTotal(ulong total)
        {
        }

        public void SetCompleted([In] ref ulong completeValue)
        {
        }

        const int S_OK = 0;
        const int S_FALSE = 1;
        const int E_ABORT = unchecked((int)0x80004004);

        ExtractFileCallbackArgs _current_file;
        OutStreamWrapper _current_stream;

        public int GetStream(uint index, [MarshalAs(UnmanagedType.Interface), Out] out ISequentialOutStream outStream, AskMode askExtractMode)
        {
            outStream = null;

            try
            {
                ExtractFileCallbackArgs a = _current_file = new ExtractFileCallbackArgs(_files[(int)index]);
                a.Reason = ExtractFileCallbackReason.Start;
                _cb(a);
                if (a.CancelExtraction)
                {
                    Canceled = true;
                    return E_ABORT;
                }

                if (askExtractMode != AskMode.Extract)
                {
                    // do not extract
                    _current_stream = null;
                    if (a.CloseStream) a.ExtractToStream?.Dispose();
                }
                else if (a.ExtractToStream != null)
                {
                    // extract to the specified stream
                    _current_stream = new OutStreamWrapper(a.ExtractToStream, a.CloseStream);
                }
                else if (a.ExtractToFile != null)
                {
                    // extract to the specified file
                    _current_stream = new OutStreamWrapper(new FileStream(a.ExtractToFile, FileMode.CreateNew,
                                                  FileAccess.Write, FileShare.Read, 8192), true);
                }
                else
                {
                    _current_stream = null;
                    System.Diagnostics.Debug.Print("Skip file");
                    // skip this file
                }

                outStream = _current_stream;
                return S_OK;
            }
            catch (Exception ex)
            {
                return Marshal.GetHRForException(ex);
            }
        }

        public void PrepareOperation(AskMode askExtractMode)
        {
        }

        public void SetOperationResult(OperationResult operationResult)
        {
            _current_stream?.Dispose();
            var a = _current_file;
            a.Reason = (operationResult == OperationResult.Ok) ? ExtractFileCallbackReason.Done : ExtractFileCallbackReason.Failure;
            _cb(a);
            if (a.CancelExtraction)
            {
                Canceled = true;
                return;
            }
        }
    }
}

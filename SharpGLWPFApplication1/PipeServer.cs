using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;

namespace MotionDrawKxt
{
    public class PipeServer
    {
        private static int numThreads = 4;
        private static ControlBox cb;

        public PipeServer(ControlBox p)
        {
            PipeServer.cb = p;
        }
        public void startServer()
        {
            int i;
            Thread[] servers = new Thread[numThreads];
            
            for (i = 0; i < numThreads; i++)
            {
                servers[i] = new Thread(ServerThread);
                servers[i].Start();
            }
            cb.print("Named pipe server started with " + numThreads + " threads");

            //while (i > 0)
            //{
            //    for (int j = 0; j < numThreads; j++)
            //    {
            //        if (servers[j] != null)
             //       {
             //           if (servers[j].Join(250))
             //           {
              ///              //Console.WriteLine("Server thread[{0}] finished.", servers[j].ManagedThreadId);
              //              servers[j] = null;
              //              i--;    // decrement the thread watch count
              ////          }
              //      }
              //  }
            //}
           // cb.print("Threads exhausted!");
        }

        private static void ServerThread(object data)
        {
            NamedPipeServerStream pipeServer;

            int threadId = Thread.CurrentThread.ManagedThreadId;


            while (true)
            {
                pipeServer = new NamedPipeServerStream("motionpipe", PipeDirection.InOut, numThreads);
                // Wait for a client to connect
                pipeServer.WaitForConnection();
                

                Console.WriteLine("Client connected on thread[{0}].", threadId);
                try
                {
                    //read client command
                    StreamString ss = new StreamString(pipeServer);
                    ss.WriteString("hello");
                    string commandString = ss.ReadString();
                    cb.print(commandString);

                }
                // Catch the IOException that is raised if the pipe is broken 
                // or disconnected. 
                catch (Exception e)
                {
                    cb.print("ERROR: "+e.Message);
                }

                pipeServer.Close();
            }

            
        }
    }

    // Defines the data protocol for reading and writing strings on our stream 
    public class StreamString
    {
        private Stream ioStream;
        private UnicodeEncoding streamEncoding;

        public StreamString(Stream ioStream)
        {
            this.ioStream = ioStream;
            streamEncoding = new UnicodeEncoding();
        }

        public string ReadString()
        {
            int len = 0;

            len = ioStream.ReadByte() * 256;
            len += ioStream.ReadByte();
            byte[] inBuffer = new byte[len];
            ioStream.Read(inBuffer, 0, len);

            return streamEncoding.GetString(inBuffer);
        }

        public int WriteString(string outString)
        {
            byte[] outBuffer = streamEncoding.GetBytes(outString);
            int len = outBuffer.Length;
            if (len > UInt16.MaxValue)
            {
                len = (int)UInt16.MaxValue;
            }
            ioStream.WriteByte((byte)(len / 256));
            ioStream.WriteByte((byte)(len & 255));
            ioStream.Write(outBuffer, 0, len);
            ioStream.Flush();

            return outBuffer.Length + 2;
        }
    }

    // Contains the method executed in the context of the impersonated user 
    public class ReadFileToStream
    {
        private string fn;
        private StreamString ss;

        public ReadFileToStream(StreamString str, string filename)
        {
            fn = filename;
            ss = str;
        }

        public void Start()
        {
            string contents = File.ReadAllText(fn);
            ss.WriteString(contents);
        }
    }

}

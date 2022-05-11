using System.Buffers;
using System.Text;

namespace BigfootLib
{
    public class StreamJsonEnumerator : IJsonEnumerator
    {
        private static readonly byte JsonBeginValue = Encoding.UTF8.GetBytes("{")[0];
        private static readonly byte JsonEndValue = Encoding.UTF8.GetBytes("}")[0];

        private readonly Stream _stream;
        private readonly byte[] _buffer;
        private int _bufferPos;
        private int _bytesInBuffer;

        public StreamJsonEnumerator(Stream stream, int bufferSize = 200)
        {
            _stream = stream;
            _buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            _bufferPos = _buffer.Length;
        }

        ~StreamJsonEnumerator()
        {
            ArrayPool<byte>.Shared.Return(_buffer);
        }

        private int FindByteInBuffer(ref int startPos, byte value, int pairingValue = -1)
        {
            int pos = startPos;
            bool hasReadFromStream = false;
            int depth = 0;

            while (true)
            {
                if (pos >= _bytesInBuffer)
                {
                    // Not found in buffer, try to refill from stream. If we have to read twice, the fragment is too large for the buffer.
                    if (hasReadFromStream)
                        throw new ArgumentException($"Buffer too small ({_buffer.Length}) to fit current JSON fragment");

                    hasReadFromStream = true;

                    int bytesToKeep = _buffer.Length - startPos;
                    int maxNewBytes = _buffer.Length - bytesToKeep;
                    Buffer.BlockCopy(_buffer, startPos, _buffer, 0, bytesToKeep);

                    int bytesRead = _stream.Read(new Span<byte>(_buffer, bytesToKeep, maxNewBytes));
                    _bytesInBuffer = bytesToKeep + bytesRead;
                    startPos = 0;
                    pos = bytesToKeep;

                    bool reachedEndOfStream = bytesRead == 0;
                    if (reachedEndOfStream)
                        return -1;
                }

                if (_buffer[pos] == pairingValue)
                {
                    depth++;
                }

                if (_buffer[pos] == value)
                {
                    if (depth-- == 0)
                        return pos;
                }

                pos++;
            }
        }

        private Span<byte> GetFragmentSpan(int startPos)
        {
            int searchStartPos = startPos;
            int fragmentStartPos = FindByteInBuffer(ref searchStartPos, JsonBeginValue);
            if (fragmentStartPos == -1)
            {
                // No new fragment found in stream.
                return Span<byte>.Empty;
            }

            int fragmentEndPos = FindByteInBuffer(ref fragmentStartPos, JsonEndValue);
            if (fragmentEndPos == -1)
            {
                // This is an error, end of current JSON fragment was not found in stream.
                return Span<byte>.Empty;
            }

            _bufferPos = fragmentEndPos + 1;

            return new Span<byte>(_buffer, fragmentStartPos, (fragmentEndPos - fragmentStartPos) + 1);
        }

        public Span<byte> GetNextJsonFragment()
        {
            return GetFragmentSpan(_bufferPos);
        }
    }
}

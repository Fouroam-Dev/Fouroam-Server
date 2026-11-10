using System.Text.Json;

namespace Fouroam.Shared;

/// <summary>Bounded newline-delimited JSON over an already secured TLS stream.</summary>
public sealed class JsonLineConnection : IDisposable
{
    public const int ProtocolVersion = 2;
    public const int MaxMessageBytes = 4096;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly byte[] Newline = [(byte)'\n'];

    private readonly Stream _stream;
    private readonly byte[] _readBuffer = new byte[2048];
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private int _readPosition;
    private int _readLength;

    public JsonLineConnection(Stream stream) =>
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));

    public async Task<WireMessage?> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        using var line = new MemoryStream();

        while (true)
        {
            if (_readPosition == _readLength)
            {
                _readLength = await _stream.ReadAsync(_readBuffer.AsMemory(), cancellationToken);
                _readPosition = 0;

                if (_readLength == 0)
                {
                    if (line.Length == 0) return null;
                    throw new InvalidDataException("Connection closed in the middle of a message.");
                }
            }

            var newlineAt = Array.IndexOf(
                _readBuffer, (byte)'\n', _readPosition, _readLength - _readPosition);
            var count = newlineAt >= 0
                ? newlineAt - _readPosition
                : _readLength - _readPosition;

            if (line.Length + count > MaxMessageBytes)
                throw new InvalidDataException("Message exceeds 4096 bytes.");

            line.Write(_readBuffer, _readPosition, count);
            _readPosition += count;
            if (newlineAt < 0) continue;

            _readPosition++;
            if (line.Length == 0)
                throw new InvalidDataException("Empty protocol message.");

            try
            {
                var message = JsonSerializer.Deserialize<WireMessage>(line.ToArray(), JsonOptions);
                if (message is null || string.IsNullOrWhiteSpace(message.Type))
                    throw new InvalidDataException("Protocol message has no type.");
                return message;
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("Invalid JSON protocol message.", ex);
            }
        }
    }

    public async Task SendAsync(WireMessage message, CancellationToken cancellationToken = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions);
        if (bytes.Length > MaxMessageBytes)
            throw new InvalidDataException("Outgoing message exceeds 4096 bytes.");

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await _stream.WriteAsync(bytes, cancellationToken);
            await _stream.WriteAsync(Newline, cancellationToken);
            await _stream.FlushAsync(cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public void Dispose() => _writeLock.Dispose();
}

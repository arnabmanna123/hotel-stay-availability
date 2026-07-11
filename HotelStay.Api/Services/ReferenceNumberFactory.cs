using System.Security.Cryptography;

namespace HotelStay.Api.Services;

public interface IReferenceNumberFactory
{
    string Generate();
}

public sealed class ReferenceNumberFactory : IReferenceNumberFactory
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int Length = 8;

    public string Generate()
    {
        Span<char> buffer = stackalloc char[Length];
        for (var i = 0; i < Length; i++)
        {
            buffer[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }
        return string.Concat("HS-", buffer);
    }
}

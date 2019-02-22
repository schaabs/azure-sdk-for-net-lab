using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Security.KeyVault
{
    public interface IKey : IDisposable
    {
        /// <summary>
        /// The default encryption algorithm for this key
        /// </summary>
        string DefaultEncryptionAlgorithm { get; }

        /// <summary>
        /// The default key wrap algorithm for this key
        /// </summary>
        string DefaultKeyWrapAlgorithm { get; }

        /// <summary>
        /// The default signature algorithm for this key
        /// </summary>
        string DefaultSignatureAlgorithm { get; }

        /// <summary>
        /// The key identifier
        /// </summary>
        string Kid { get; }

        /// <summary>
        /// Decrypts the specified cipher text.
        /// </summary>
        /// <param name="ciphertext">The cipher text to decrypt</param>
        /// <param name="iv">The initialization vector</param>
        /// <param name="authenticationData">The authentication data</param>
        /// <param name="algorithm">The algorithm to use</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>The plain text</returns>
        /// <remarks>If algorithm is not specified, an implementation should use its default algorithm.
        /// Not all algorithms require, or support, all parameters.</remarks>
        Task<byte[]> DecryptAsync(Stream ciphertext, byte[] iv, byte[] authenticationData, byte[] authenticationTag, string algorithm, CancellationToken token = default);

        /// <summary>
        /// Encrypts the specified plain text.
        /// </summary>
        /// <param name="plaintext">The plain text to encrypt</param>
        /// <param name="iv">The initialization vector</param>
        /// <param name="authenticationData">The authentication data</param>
        /// <param name="algorithm">The algorithm to use</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>A Tuple consisting of the cipher text, the authentication tag (if applicable), the algorithm used</returns>
        /// <remarks>If the algorithm is not specified, an implementation should use its default algorithm.
        /// Not all algorithyms require, or support, all parameters.</remarks>
        Task<Tuple<byte[], byte[], string>> EncryptAsync(Stream plaintext, byte[] iv, byte[] authenticationData, string algorithm, CancellationToken token = default);

        /// <summary>
        /// Encrypts the specified key material.
        /// </summary>
        /// <param name="key">The key material to encrypt</param>
        /// <param name="algorithm">The algorithm to use</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>A Tuple consisting of the encrypted key and the algorithm used</returns>
        /// <remarks>If the algorithm is not specified, an implementation should use its default algorithm</remarks>
        Task<Tuple<byte[], string>> WrapKeyAsync(byte[] key, string algorithm, CancellationToken token = default);

        /// <summary>
        /// Decrypts the specified key material.
        /// </summary>
        /// <param name="encryptedKey">The encrypted key material</param>
        /// <param name="algorithm">The algorithm to use</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>The decrypted key material</returns>
        /// <remarks>If the algorithm is not specified, an implementation should use its default algorithm</remarks>
        Task<byte[]> UnwrapKeyAsync(byte[] encryptedKey, string algorithm, CancellationToken token = default);

        /// <summary>
        /// Signs the specified digest.
        /// </summary>
        /// <param name="digest">The data to sign</param>
        /// <param name="algorithm">The algorithm to use</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>A Tuple consisting of the signature and the algorithm used</returns>
        /// <remarks>If the algorithm is not specified, an implementation should use its default algorithm</remarks>
        Task<Tuple<byte[], string>> SignAsync(Stream data, string algorithm, CancellationToken token = default);

        /// <summary>
        /// Verifies the specified signature value
        /// </summary>
        /// <param name="digest">The digest</param>
        /// <param name="signature">The signature value</param>
        /// <param name="algorithm">The algorithm to use</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>A bool indicating whether the signature was successfully verified</returns>
        Task<bool> VerifyAsync(Stream data, byte[] signature, string algorithm, CancellationToken token = default);

        /// <summary>
        /// Signs the specified digest.
        /// </summary>
        /// <param name="digest">The digest to sign</param>
        /// <param name="algorithm">The algorithm to use</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>A Tuple consisting of the signature and the algorithm used</returns>
        /// <remarks>If the algorithm is not specified, an implementation should use its default algorithm</remarks>
        Task<Tuple<byte[], string>> SignDigestAsync(byte[] digest, string algorithm, CancellationToken token = default);

        /// <summary>
        /// Verifies the specified signature value
        /// </summary>
        /// <param name="digest">The digest</param>
        /// <param name="signature">The signature value</param>
        /// <param name="algorithm">The algorithm to use</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>A bool indicating whether the signature was successfully verified</returns>
        Task<bool> VerifyDigestAsync(byte[] digest, byte[] signature, string algorithm, CancellationToken token = default); 
    }
}

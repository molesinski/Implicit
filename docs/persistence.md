# Binary Persistence

ALS models and weighting transforms have separate `Save(Stream)` and static `Load(Stream)` methods. They use .NET `BinaryWriter` and `BinaryReader` with UTF-8 encoding and `leaveOpen: true`.

Sources: [ALS persistence](../src/Implicit/AlternatingLeastSquaresRecommender.cs), [TF-IDF persistence](../src/Implicit/Weights/TFIDFWeight.cs), and [BM25 persistence](../src/Implicit/Weights/BM25Weight.cs).

## Stream contract

Saving writes at the stream's current position. Loading reads from its current position. Neither operation rewinds or truncates the stream, and disposing the internal reader or writer leaves the caller's stream open. Reset position before reading back from a `MemoryStream`; use a newly created or explicitly truncated file when replacing an artifact.

```csharp
using (var output = File.Create("model.bin"))
{
    model.Save(output);
}

using var input = File.OpenRead("model.bin");
var restored = AlternatingLeastSquaresRecommender.Load(input);
```

Null streams are rejected. Stream capability and data errors propagate from the underlying IO and matrix operations.

## Primitive encoding

Integers are 32-bit little-endian signed values. Factors and parameters are IEEE 754 single-precision values written in little-endian order. Strings use `BinaryWriter.Write(string)`: a 7-bit encoded byte-length prefix followed by UTF-8 bytes, without a terminator.

## ALS model layout

| Order | Field | Encoding |
| --- | --- | --- |
| 1 | Factor count `F` | Int32 |
| 2 | Regularization | Single |
| 3 | Alpha | Single |
| 4 | User count `U` | Int32 |
| 5 | Item count `I` | Int32 |
| 6 | Each user record, repeated `U` times | String ID, followed by `F` Singles |
| 7 | Each item record, repeated `I` times | String ID, followed by `F` Singles |

Saving enumerates each map and reads the factor row addressed by its value. Loading assigns consecutive row indices in serialized record order. The file does not serialize map index values separately. User/item associations are preserved, but consumers should use the loaded maps rather than assuming old row indices are externally stable.

The model retains factors, IDs, regularization, and alpha. It does not retain interaction history, weighting transforms, random state, iteration count, solver choice, callbacks, parallel options, norms, or Gram matrix caches. Caches are computed again on demand. Loading supports querying and temporary feature inference; it does not provide a training-resumption API.

## Weight layouts

TF-IDF writes an Int32 IDF entry count, followed by that many `(String itemId, Single idf)` records.

BM25 writes three Singles (`k1`, `b`, `averageLength`), then an Int32 IDF entry count and the same IDF records. The original fitting rows are not saved.

The caller must know which type to load. If a deployment uses a fitted weight transform, save it alongside the ALS model and apply that same transform to inference histories before calling `ComputeUserFeatures` or `ComputeItemFeatures`.

## Compatibility and integrity

There is no magic header, format version, type discriminator, checksum, or trailing-data check. Loaders trust declared dimensions and counts and can allocate memory before detecting malformed input. Use artifacts from a trusted source and validate application-level size limits before loading externally supplied data.

There is no migration layer or explicit cross-version compatibility guarantee. When evolving the layout, add version handling and tests for old artifacts rather than assuming round-trip tests establish backward compatibility. Dictionary enumeration order is not a canonical serialization contract, so equivalent models need not have byte-identical files.

For production model bundles, keep application metadata alongside the binaries: producing library version, confidence/weighting configuration, and the IDs of the model and weight artifacts. This metadata is the application's responsibility; it is not embedded by the library.

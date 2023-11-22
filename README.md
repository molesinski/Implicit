# Implicit

[![CI](https://dev.azure.com/molesinski/implicit/_apis/build/status/implicit?branchName=master)](https://dev.azure.com/molesinski/implicit/_build/latest?definitionId=1&branchName=master)
[![NuGet](https://img.shields.io/nuget/dt/implicit.svg)](https://www.nuget.org/packages/implicit)
[![NuGet](https://img.shields.io/nuget/v/implicit.svg)](https://www.nuget.org/packages/implicit)
[![License](https://img.shields.io/github/license/molesinski/implicit.svg)](https://github.com/molesinski/implicit/blob/master/LICENSE)

Fast .NET Collaborative Filtering for implicit Datasets.

## Based on

This project is based on Python library [Implicit](https://github.com/benfred/implicit) by [Ben Frederickson](https://github.com/benfred).
Therefore provides fast C# implementations of several (so far only ALS) different popular recommendation algorithms for implicit feedback datasets:

 * Alternating Least Squares as described in the papers [Collaborative Filtering for implicit Feedback Datasets](http://yifanhu.net/PUB/cf.pdf) and [Applications of the Conjugate Gradient Method for implicit Feedback Collaborative Filtering](https://pdfs.semanticscholar.org/bfdf/7af6cf7fd7bb5e6b6db5bbd91be11597eaf0.pdf).
 * (PLANNED) [Bayesian Personalized Ranking](https://arxiv.org/pdf/1205.2618.pdf).
 * (PLANNED) Item-Item Nearest Neighbour models using Cosine, TFIDF or BM25 as a distance metric.

All models have multi-threaded training routines, using Parallel execution to fit the models in parallel among all available CPU cores. Library uses [MathNet.Numerics](https://github.com/mathnet/mathnet-numerics) to perform linear algebra calculations, so it is possible to use Intel Math Kernel Library (MKL) to speed up computations by following [this](https://numerics.mathdotnet.com/MKL.html#Intel-Math-Kernel-Library-MKL) guide.

## License

- Implicit, [MIT License](LICENSE)

Implicit
========

[![Build status](https://ci.appveyor.com/api/projects/status/1214xw9xk8ts0j9e?svg=true)](https://ci.appveyor.com/project/MaciejOlesiski/implicit)
[![License MIT](https://img.shields.io/github/license/molesinski/implicit.svg)](https://github.com/molesinski/Implicit/blob/master/LICENSE)

Fast .NET Collaborative Filtering for Implicit Datasets.

This project is based on Python library [Implicit](https://github.com/benfred/implicit) by [Ben Frederickson](https://github.com/benfred).
Therefore provides fast C# implementations of several (so far only ALS) different popular recommendation algorithms for implicit feedback datasets:

 * Alternating Least Squares as described in the papers [Collaborative Filtering for Implicit Feedback Datasets](http://yifanhu.net/PUB/cf.pdf) and [Applications of the Conjugate Gradient Method for Implicit Feedback Collaborative Filtering](https://pdfs.semanticscholar.org/bfdf/7af6cf7fd7bb5e6b6db5bbd91be11597eaf0.pdf).
 * (PLANNED) [Bayesian Personalized Ranking](https://arxiv.org/pdf/1205.2618.pdf).
 * (PLANNED) Item-Item Nearest Neighbour models using Cosine, TFIDF or BM25 as a distance metric.

All models have multi-threaded training routines, using Parallel execution to fit the models in parallel among all available CPU cores. Library uses [MathNet.Numerics](https://github.com/mathnet/mathnet-numerics) to perform linear algebra calculations, so it is possible to use Intel Math Kernel Library (MKL) to speed up computations by following [this](https://numerics.mathdotnet.com/MKL.html#Intel-Math-Kernel-Library-MKL) guide.

Released under the MIT License

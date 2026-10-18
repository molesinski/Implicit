# Implicit Developer Documentation

Implicit is a C# library for collaborative filtering from implicit feedback. It learns dense user and item factors with alternating least squares (ALS), then uses those factors for recommendations, similarity queries, and inference from new interaction histories.

## Reading guide

| Document | Purpose |
| --- | --- |
| [Getting started](getting-started.md) | Train, query, filter, and persist a model with a complete example. |
| [Architecture](architecture.md) | Understand the projects, type hierarchy, data flow, memory ownership, and concurrency. |
| [Data and weighting](data-and-weighting.md) | Prepare interactions, interpret confidence values, and apply TF-IDF or BM25. |
| [ALS implementation](als.md) | Follow the objective, solvers, parameter defaults, iteration callbacks, and loss calculation. |
| [API behavior](api.md) | Use feature inference and ranking APIs correctly, including result lifetimes. |
| [Persistence](persistence.md) | Read the binary layouts and understand what a saved model preserves. |
| [Development and testing](development.md) | Build all targets, run tests and benchmarks, and understand the release pipeline. |
| [Engineering considerations](engineering-notes.md) | Review implementation constraints, test gaps, and changes that require special care. |

Start with the example when integrating the library. Read architecture and ALS before modifying training. Consult API behavior and persistence before building a serving layer.

## Implementation scope

ALS is the only concrete recommender. TF-IDF and BM25 are preprocessing utilities, not separate recommendation algorithms. Bayesian Personalized Ranking and standalone item-neighbor models are listed as planned work in the [repository README](../README.md).

The library stores model factors in single-precision MathNet matrices and uses double-precision working matrices for direct ALS solves. It contains no HTTP server, database adapter, application configuration system, or built-in recommendation evaluation pipeline. Applications own data acquisition, filtering, deployment, and relevance evaluation.

For contribution conventions, see [Repository Guidelines](../AGENTS.md). Each technical chapter links to the relevant implementation files.

Hi Folks,

This library is another result of my entusiasm and compulsion to computer algoritms.

Ellaborated in arround 34 hours without stops, except stops for food, personal hygiene and sleep, had its origin in my employer's need and my difficulty in reach accuracy results of Bayesian calculum on C# implemented libraries.

It is a simple implementation of a parallelized hashed-words dictionary and a internal map-reduce schema.
Some good performance arrived : 1 million records per minute on data training (with disabled phonetic match), into 864 data groups at an Dell Intel i7 3rd generation scenario.

Limitation : The library supports around 3.3 million records processing per instance (data structure capacity), but you can apply an offset on your full data, and work with multiple knowledge base dumps (only about 10mb per million records on native project).

I humbly make it available to you.

Enjoy it!

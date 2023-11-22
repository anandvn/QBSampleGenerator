# QBSampleGenerator
Generate Invoice Samples from Quickbooks datafile

## Purpose
Generate Samples to be used in Machine Learning Models from a Quickbooks datafile.  Quickbooks desktop allows
a user to "attach" documents to records as they are posted to Quickbooks. This allows use to create labeled samples 
using the data already human verified inside Quickbooks.

### Credits
* ProgressBar https://gist.github.com/DanielSWolf
* Command line parser https://github.com/commandlineparser/commandline
* QBSDK: https://developer.intuit.com/app/developer/qbdesktop/docs/get-started
* QBSDK Programmers Guide: https://static.developer.intuit.com/qbSDK-current/doc/pdf/QBSDK_ProGuide.pdf
* Session Framework code is taken wholesale from the C# template provided by Intuit, with some modifications.

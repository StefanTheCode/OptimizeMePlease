# OptimizeMePlease

## You are probably here because you saw my post on Linkedin.
## Welcome!

# Steps

- Before you run an application, you will need to create a database (I'm using MSSQL) named "OptimizeMePlease"
- Go to Program.cs class
- "IWillPopulateDate()" is a method which will get a script from the project directory and run in on created DB
- Run application in Debug/Release mode
- Comment or delete IWillPopulateData() call from Main method
- Go to BenchmarkService.cs class
- Start coding within GetAuthors_Optimized method

# How do I submit my solution?

- Clone the project, create a branch and work on that branch.

### OR

- If you don't want to bother with github, after you're done with the changes, send me the results on Linkedin.

# Rules

- Only Entity Framework (Core) is allowed for using
- The data obtained in the non-optimized version of the code must also be obtained in the optimized version
- If you see potential optimization of something else, you can do it
- Entities and DbContext cannot be changed (you got legacy code no matter what it might be bad :) )
- The models returned from the method can be changed

# What should the method return?

- Given that there is a predefined database of data, the method should in any case return the list of data currently returned by the non-optimized method.

# How will performance success be measured?

- Given that each of us works on a computer with different performance and power, the execution time quotient of the non-optimized and optimized method will be checked for each separately.

Example:

Non-optimized method Execution time: 1.1s = 1100ms
Optimized method Execution time: 200ms

### Result: 1100ms/200ms = 5.5x faster.

In addition, I will check the performance on my computer for individual results.

# GOOD LUCK!

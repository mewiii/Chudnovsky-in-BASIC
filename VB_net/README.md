# Binary Splitting Implementation of the Chudnosky algorithm

## Example execution of program to 50 digits without rounding on the last digit
```
deanna ~/Development/BSpiVB $ dotnet run -- --digits 50 --rounding truncate
3.14159265358979323846264338327950288419716939937510
[digits=50, rounding=Truncate, elapsed=00:00:00.0031861]
deanna ~/Development/BSpiVB $
```

## Example execution of program to 50 digits with rounding on the last digit
```
deanna ~/Development/BSpiVB $ dotnet run -- --digits 50 --rounding round      
3.14159265358979323846264338327950288419716939937511
[digits=50, rounding=Round, elapsed=00:00:00.0030357]
deanna ~/Development/BSpiVB $ 
```

These outputs are easily verified using online sources of large numbers of digits of Pi, for example,
[100,000 Digits of Pi](http://www.geom.uiuc.edu/~huberty/math5337/groupe/digits.html) by
[Michael D. Huberty](https://www.linkedin.com/in/michael-huberty-21612614a/),
Chia Vang, and Ko Hayashi, of the University of Minnesota, circa 1997.

' Created by: Deanna M. Wilborne
' Compute Pi using Binary Splitting of the
' Chudnovsky algorithm.
'
' Developed and tested on MacBook Pro ARM64 using dotnet commands


Option Strict On
Option Infer On

Imports System
Imports System.Numerics
Imports System.Diagnostics

Module Program

    ' ---- BigInteger constants ----
    Private ReadOnly BI2  As BigInteger = 2
    Private ReadOnly BI10 As BigInteger = 10
    Private ReadOnly BI24 As BigInteger = 24

    Private ReadOnly Acoef   As BigInteger = 13591409
    Private ReadOnly Bcoef   As BigInteger = 545140134
    Private ReadOnly Cconst  As BigInteger = 640320
    Private ReadOnly C3      As BigInteger = Cconst * Cconst * Cconst
    Private ReadOnly C3_OVER_24 As BigInteger = BigInteger.Divide(C3, BI24)

    Private ReadOnly K426880 As BigInteger = 426880
    Private ReadOnly K10005  As BigInteger = 10005

    Private Enum RoundingMode
        Round = 0
        Truncate = 1
    End Enum

    ' (P, Q, T) triple
    Private Structure PQT
        Public P As BigInteger
        Public Q As BigInteger
        Public T As BigInteger
        Public Sub New(p As BigInteger, q As BigInteger, t As BigInteger)
            Me.P = p : Me.Q = q : Me.T = t
        End Sub
    End Structure

    Sub Main(args As String())
        Dim digits As Integer = 1000
        Dim mode As RoundingMode = RoundingMode.Round   ' default

        For i = 0 To args.Length - 1
            Select Case args(i)
                Case "--digits", "-d"
                    If i + 1 < args.Length AndAlso Integer.TryParse(args(i + 1), digits) Then
                        i += 1
                    Else
                        Console.Error.WriteLine("Usage: dotnet run -- [--digits N] [--rounding round|truncate]")
                        Return
                    End If
                Case "--rounding"
                    If i + 1 < args.Length Then
                        Dim val = args(i + 1).ToLowerInvariant()
                        If val = "round" Then
                            mode = RoundingMode.Round
                        ElseIf val = "truncate" Then
                            mode = RoundingMode.Truncate
                        Else
                            Console.Error.WriteLine("Error: --rounding must be 'round' or 'truncate'.")
                            Return
                        End If
                        i += 1
                    Else
                        Console.Error.WriteLine("Error: --rounding requires an argument: round|truncate")
                        Return
                    End If
            End Select
        Next
        If digits < 1 Then digits = 1

        Dim sw As New Stopwatch()
        sw.Start()
        Dim piText As String = ComputePi(digits, mode)   ' pass mode through
        sw.Stop()

        Console.WriteLine(piText)
        Console.Error.WriteLine($"[digits={digits}, rounding={mode}, elapsed={sw.Elapsed}]")
    End Sub

    ' Private Function ComputePi(digits As Integer) As String
    Private Function ComputePi(digits As Integer, mode As RoundingMode) As String
        ' ~14.181647... digits per term; add small guard
        Dim guardDigits As Integer = 10
        Dim terms As Integer = CInt(Math.Ceiling((digits + guardDigits) / 14.181647462725477R))
        If terms < 1 Then terms = 1

        ' Binary split over k = 0..terms-1 (inclusive of k=0 via leaf)
        Dim pqt As PQT = BS(0, terms)
        ' Console.Error.WriteLine($"[const] Acoef={Acoef} Bcoef={Bcoef}")
        ' Console.Error.WriteLine($"[diag] terms={terms}")
        ' Console.Error.WriteLine($"[diag] Q.sign={pqt.Q.Sign}, T.sign={pqt.T.Sign}")

        For testN As Integer = 1 To Math.Min(5, terms)
            Dim tPQT = BS(0, testN)
            ' Console.Error.WriteLine($"[diag] n={testN}  T.sign={tPQT.T.Sign}")
        Next

        ' Series sum S = T / Q
        Dim Ttop As BigInteger = pqt.T
        Dim Qtop As BigInteger = pqt.Q

        ' sqrt(10005) scaled by 10^(digits+guardDigits)
        Dim scale As Integer = digits + guardDigits
        Dim sqrtScaled As BigInteger = ISqrt(K10005 * Pow10BI(2 * scale))

        ' π ≈ (Q * 426880 * sqrt(10005)) / T, all integers with rounding
        Dim numer As BigInteger = Qtop * K426880 * sqrtScaled
        Dim denom As BigInteger = Ttop

        Dim piScaled As BigInteger = BigInteger.Divide(numer + BigInteger.Divide(denom, BI2), denom)

        ' Now drop guard digits using requested mode
        If guardDigits > 0 Then
            Dim tenGuard As BigInteger = Pow10BI(guardDigits)
            If mode = RoundingMode.Round Then
                piScaled = BigInteger.Divide(piScaled + BigInteger.Divide(tenGuard, BI2), tenGuard)
            Else
                ' Truncate
                piScaled = BigInteger.Divide(piScaled, tenGuard)
            End If
        End If

        Return FormatScaled(piScaled, digits)
    End Function

    ' Binary splitting on [a, b): returns P,Q,T for k = a .. b-1
    Private Function BS(a As Integer, b As Integer) As PQT
        If b - a = 1 Then
            Dim k As Integer = b - 1
            If k = 0 Then
                Dim res0 As New PQT(BigInteger.One, BigInteger.One, Acoef)
                ' Console.Error.WriteLine($"[leaf] a={a} b={b} k=0  P=1 Q=1 T=A({A})")
                Return res0
            End If

            Dim kBI As New BigInteger(k)
            Dim P As BigInteger = (6 * kBI - 5) * (2 * kBI - 1) * (6 * kBI - 1)
            Dim Q As BigInteger = kBI * kBI * kBI * C3_OVER_24
            Dim T As BigInteger = P * (Acoef + Bcoef * kBI)
            If (k And 1) = 1 Then T = -T

            ' Console.Error.WriteLine($"[leaf] a={a} b={b} k={k}  sign(P)={P.Sign} sign(Q)={Q.Sign} sign(T)={T.Sign}")
            Return New PQT(P, Q, T)
        Else
            Dim m As Integer = (a + b) \ 2
            Dim left As PQT = BS(a, m)
            Dim right As PQT = BS(m, b)

            Dim P As BigInteger = left.P * right.P
            Dim Q As BigInteger = left.Q * right.Q
            Dim T As BigInteger = left.T * right.Q + left.P * right.T

            ' Only spam merges for small ranges so we don't flood output
            ' If b - a <= 4 Then
            '     Console.Error.WriteLine($"[merge] [{a},{b})  sign(L.T)={left.T.Sign} sign(R.T)={right.T.Sign}  sign(T)={T.Sign}")
            ' End If

            Return New PQT(P, Q, T)
        End If
    End Function

    ' Integer sqrt: floor(sqrt(n))
    Private Function ISqrt(n As BigInteger) As BigInteger
        If n.Sign <= 0 Then Return BigInteger.Zero
        Dim x As BigInteger = n
        Dim y As BigInteger = BigInteger.Divide(x + 1, BI2)
        While y < x
            x = y
            y = BigInteger.Divide(y + BigInteger.Divide(n, y), BI2)
        End While
        Return x
    End Function

    ' 10^k as BigInteger
    Private Function Pow10BI(k As Integer) As BigInteger
        If k <= 0 Then Return BigInteger.One
        Return BigInteger.Pow(BI10, k)
    End Function

    ' Insert decimal point: v is floor(π * 10^digits)
    Private Function FormatScaled(v As BigInteger, digits As Integer) As String
        Dim s As String = v.ToString()
        If digits = 0 Then Return s
        If s.Length <= digits Then
            s = New String("0"c, digits - s.Length + 1) & s
        End If
        Dim intPart As String = s.Substring(0, s.Length - digits)
        Dim fracPart As String = s.Substring(s.Length - digits)
        Return intPart & "." & fracPart
    End Function
End Module

Imports System

'Source: https://docs.microsoft.com/en-us/dotnet/visual-basic/programming-guide/concepts/collections
Public Class Galaxy
    Public Property Name As String
    Public Property MegaLightYears As Integer
End Class

Module Program

Sub IterateThroughList()
    Dim theGalaxies As New List(Of Galaxy) From
        {
            New Galaxy With {.Name = "Tadpole", .MegaLightYears = 400},
            New Galaxy With {.Name = "Pinwheel", .MegaLightYears = 25},
            New Galaxy With {.Name = "Milky Way", .MegaLightYears = 0},
            New Galaxy With {.Name = "Andromeda", .MegaLightYears = 3}
        }

    For Each theGalaxy As Galaxy In theGalaxies
        With theGalaxy
            Console.WriteLine(.Name & "  " & .MegaLightYears)
        End With
    Next

End Sub

'Source: https://docs.microsoft.com/en-us/dotnet/core/tutorials/with-visual-studio?pivots=dotnet-6-0
    Sub Main(args As String())
        Console.WriteLine("Hello World!")
    End Sub

End Module

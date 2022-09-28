Public Class Dummy
	Public Property BaseSelectCommand As String
	Public Property BaseConnectionString As String
	Public Property BaseTableName As String
	Public Property BaseSQLCommand As String
End Class

Public Class Settings
	Inherits Dummy
	Public Property SQLCommand As String
	Public Property ConnectionString As String
	Public Property TableName As String
	Public Property Dummy1 As New Dummy1
	Public Property SelectCommand As String
End Class

Public Class Dummy1
	Public Property A As String
	Public Property Z As String
	Public Property T As String
	Public Property B As String
End Class

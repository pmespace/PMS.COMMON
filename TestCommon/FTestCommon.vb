Imports System.Data.OleDb
Imports COMMON

Public Class FTestCommon
	Private database As New CDatabase
	Private json As New CJson(Of Settings)
	Private Const CONNECT As String = "Connect database"
	Private Const DISCONNECT As String = "Disconnect database"

	Private Sub FTestCommon_Load(sender As Object, e As EventArgs) Handles MyBase.Load
		json.FileName = "testcommon.settings.json"
		ReadSettings()
		lblSelectRes.Text = String.Empty
		lblSQLNbRows.Text = String.Empty
		lblSQLRes.Text = String.Empty
		SetButtons()
	End Sub

	Private Sub ReadSettings()
		Dim settings = json.ReadSettings()

		If Not IsNothing(settings) Then
			efConnectionString.Text = settings.ConnectionString
			efAnyRequest.Text = settings.SQLCommand
			efSelect.Text = settings.SelectCommand
		End If
	End Sub

	Private Sub WriteSettings()
		Dim settings As New Settings
		settings.ConnectionString = efConnectionString.Text
		settings.SQLCommand = efAnyRequest.Text
		settings.SelectCommand = efSelect.Text

		json.WriteSettings(settings)
	End Sub

	Private Sub SetButtons()
		pnlSQL.Enabled = database.IsOpen
		If database.IsOpen Then
			pbTestConnectionString.Text = DISCONNECT
		Else
			pbTestConnectionString.Text = CONNECT
		End If
	End Sub

	Private Sub pbTestConnectionString_Click(sender As Object, e As EventArgs) Handles pbTestConnectionString.Click
		If database.IsOpen Then
			database.IsOpen = False
			If database.IsOpen Then
				efConnectionString.BackColor = Color.Crimson
			Else
				efConnectionString.BackColor = SystemColors.Window
			End If
		Else
			database.ConnectionString = efConnectionString.Text
			If database.IsOpen Then
				efConnectionString.BackColor = Color.LightGreen
			Else
				efConnectionString.BackColor = Color.Crimson
			End If
		End If
		SetButtons()
	End Sub

	Private Sub pbAnyRequest_Click(sender As Object, e As EventArgs) Handles pbAnyRequest.Click
		Dim nbRows As Integer
		If database.NonSelectRequest(efAnyRequest.Text, nbRows) Then
			lblSQLRes.BackColor = Color.Transparent
			lblSQLRes.ForeColor = SystemColors.ControlText
			lblSQLRes.Text = "OK"
			lblSQLNbRows.Text = nbRows.ToString
		Else
			lblSQLRes.BackColor = Color.Crimson
			lblSQLRes.ForeColor = Color.Yellow
			lblSQLRes.Text = "KO"
			lblSQLNbRows.Text = String.Empty
		End If
	End Sub

	Private Sub pbSelect_Click(sender As Object, e As EventArgs) Handles pbSelect.Click
		Dim ds As New DataSet
		If database.SelectRequest(efSelect.Text, DataSet1) Then
			lblSelectRes.BackColor = Color.Transparent
			lblSelectRes.ForeColor = SystemColors.ControlText
			lblSelectRes.Text = "OK"
			'display content of select request
			DataGridView1.Columns.Clear()
			DataGridView1.AutoGenerateColumns = True
			DataGridView1.DataSource = DataSet1.Tables(0).DefaultView
		Else
			lblSelectRes.ForeColor = Color.Crimson
			lblSelectRes.ForeColor = Color.Yellow
			lblSelectRes.Text = "KO"
		End If

		'Dim ds As New DataSet
		'Dim l As List(Of Object)
		'l = database.SelectRequest(Of Object)(efSelect.Text, AddressOf Feed)
		'If Not IsNothing(l) Then
		'	lblSelectRes.BackColor = Color.Transparent
		'	lblSelectRes.ForeColor = SystemColors.ControlText
		'	lblSelectRes.Text = "OK"
		'Else
		'	lblSelectRes.ForeColor = Color.Crimson
		'	lblSelectRes.ForeColor = Color.Yellow
		'	lblSelectRes.Text = "KO"
		'End If
	End Sub

	Private Function Feed(reader As OleDbDataReader) As Object

		Return New Object
	End Function

	Private Sub FTestCommon_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
		WriteSettings()
	End Sub

	Private Sub pbSaveSettings_Click(sender As Object, e As EventArgs) Handles pbSaveSettings.Click
		WriteSettings()
	End Sub

	Private Sub pbClose_Click(sender As Object, e As EventArgs) Handles pbClose.Click
		Close()
	End Sub
End Class

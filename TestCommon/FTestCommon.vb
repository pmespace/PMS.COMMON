Imports System.Data.OleDb
Imports System.IO
Imports System.Text
Imports System.Xml
Imports System.Xml.Serialization
Imports COMMON
Imports Newtonsoft.Json

Public Class FTestCommon
	Private database As New CDatabase
	Private json As New CJson(Of Settings)
	Private Const CONNECT As String = "Connect database"
	Private Const DISCONNECT As String = "Disconnect database"
	Private DbM As New CDatabaseTableManager
	Private DataTable As DataTable

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
			efTableName.Text = settings.TableName
		End If
	End Sub

	Private Sub WriteSettings()
		Dim settings As New Settings
		settings.ConnectionString = efConnectionString.Text
		settings.SQLCommand = efAnyRequest.Text
		settings.SelectCommand = efSelect.Text
		settings.TableName = efTableName.Text

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
		Dim ds As DataSet = Nothing
		If database.SelectRequest(efSelect.Text, ds) Then
			lblSelectRes.BackColor = Color.Transparent
			lblSelectRes.ForeColor = SystemColors.ControlText
			lblSelectRes.Text = "OK"
			'display content of select request
			DataGridView1.Columns.Clear()
			DataGridView1.AutoGenerateColumns = True
			DataGridView1.DataSource = ds.Tables(0).DefaultView
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

	Private Sub pbTable_Click(sender As Object, e As EventArgs) Handles pbTable.Click
		DbM.ConnectionString = database.ConnectionString
		DataTable = DbM.FillTable($"SELECT * FROM {efTableName.Text}")
		DataGridView1.Columns.Clear()
		DataGridView1.AutoGenerateColumns = True
		DataGridView1.DataSource = DataTable
	End Sub

	Private Sub efConnectionString_TextChanged(sender As Object, e As EventArgs) Handles efConnectionString.TextChanged
		efConnectionString.BackColor = SystemColors.Window
		database.IsOpen = False
		DataGridView1.DataSource = Nothing
		SetButtons()
	End Sub

	Class ConnectRequestData
		Public Property ICCD As String
		Public Property user As String
		Public Property password As String
		Public Property port As Integer
	End Class

	Class ConnectRequest
		Public Sub New()
			connect = New ConnectRequestData
		End Sub
		Public connect As ConnectRequestData
	End Class

	<Serializable, XmlRoot(ElementName:="connect")>
	Class ConnectReplyData
		Public Property status As Integer
	End Class
	Private Const CONNECT_STATUS_OK As Integer = 0
	Private Const CONNECT_STATUS_KO As Integer = -1

	Class ConnectReply
		Public Sub New()
			connect = New ConnectReplyData
		End Sub
		Public Property connect As ConnectReplyData
	End Class

	Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
		lblResult.Visible = False
		lblResult.Text = "KO"
		'create the XML request
		Dim request As New ConnectRequest
		request.connect.ICCD = "9517039264045"
		request.connect.user = "JTHURIN"
		request.connect.password = "AThenes2004"
		request.connect.port = 2018
		Dim xml As XmlDocument = JsonConvert.DeserializeXmlNode(JsonConvert.SerializeObject(request))
		Dim clientSettings As New CStreamClientSettings() With
			{
			.IP = "194.50.38.6",
			.Port = 3470,
			.ReceiveTimeout = 0,
			.CheckCertificate = True,
			.ServerName = "sslstca.lyra-network.com"
			}
		'send xml request waiting for an xml reply
		Dim err As Boolean
		Dim s As String = xml.InnerXml
		Dim xmls As String = CStream.ConnectSendReceiveLine(clientSettings, s, err)
		If Not String.IsNullOrEmpty(xmls) Then
			Try
				'deserialize the reply to a structure

				'remove version
				Dim xmlSetting As New XmlReaderSettings()
				xmlSetting.IgnoreComments = True
				xmlSetting.IgnoreProcessingInstructions = True
				xmlSetting.IgnoreWhitespace = True
				xmlSetting.CloseInput = True

				Dim xsSubmit As New XmlSerializer(GetType(ConnectReplyData))
				Dim stream As New StreamReader(New MemoryStream(Encoding.UTF8.GetBytes(xmls)), Encoding.UTF8, False)
				Dim reader As XmlReader = XmlReader.Create(stream, xmlSetting)
				Dim reply As ConnectReplyData = xsSubmit.Deserialize(reader)
				If Not IsNothing(reply) Then
					'test the result
					If 0 = reply.status Then
						lblResult.Text = "OK"
					Else
						lblResult.Text = $"KO [{reply.status}]"
					End If
				Else
					'error, not a valid object
				End If
			Catch ex As Exception

			End Try
			'test the returned status

		Else
			'error connecting to the gateway
		End If
		lblResult.Visible = True
	End Sub

	Private Sub pbNbRows_Click(sender As Object, e As EventArgs) Handles pbNbRows.Click
		lblTableNbRows.Text = database.NbRows(efTableName.Text)
	End Sub
End Class

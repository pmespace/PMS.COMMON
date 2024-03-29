﻿Imports System.Data.Odbc
Imports System.IO
Imports System.Text
Imports System.Threading
Imports System.Xml
Imports System.Xml.Serialization
Imports COMMON
Imports COMMON.ODBC
Imports COMMON.WIN32
Imports Newtonsoft.Json

Public Class FTestCommon
	Private database As New CDatabase
	Private json As New CJson(Of Settings)
	Private serial As New JsonSerializerSettings
	Private Const CONNECT As String = "Connect database"
	Private Const DISCONNECT As String = "Disconnect database"
	Private DbM As New CDatabaseTableManager
	Private DataTable As DataTable
	Private myThread As CThread
	Private myEvent As AutoResetEvent = New AutoResetEvent(False)

	Private Sub FTestCommon_Load(sender As Object, e As EventArgs) Handles MyBase.Load
		json.FileName = "..\testcommon.settings.json"
		LoadSettings()
		lblSelectRes.Text = String.Empty
		lblSQLNbRows.Text = String.Empty
		lblSQLRes.Text = String.Empty
		SetButtons()
		Dim visible As Boolean = False
		CLog.Filename = "test.log"
		CLog.Add($"Starting testcommon")
#If DEBUG Then
		visible = True
#End If
		pbHex.Visible = visible
		pbOther.Visible = visible

	End Sub

	Private Sub LoadSettings()
		Dim settings As Settings
		If Not cbNewJson.Checked Then
			settings = json.ReadSettings()
		Else
			Dim except As Exception = Nothing
			settings = json.ReadSettings(serial)
		End If

		If Not IsNothing(settings) Then
			efConnectionString.Text = settings.ConnectionString
			efAnyRequest.Text = settings.SQLCommand
			efSelect.Text = settings.SelectCommand
			efTableName.Text = settings.TableName
		End If
	End Sub

	Private Sub SaveSettings()
		Dim settings As New Settings
		settings.ConnectionString = efConnectionString.Text
		settings.SQLCommand = efAnyRequest.Text
		settings.SelectCommand = efSelect.Text
		settings.TableName = efTableName.Text

		If Not cbNewJson.Checked Then
			Dim addnull As Boolean = True
			json.WriteSettings(settings)
		Else
			Dim except As Exception = Nothing
			If rbAlphabetical.Checked Then
				serial = json.SerializeAlphabetically(serial)
			ElseIf rbBaseClass.Checked Then
				serial = json.SerializeBaseClassFirst(serial)
			Else
				serial = json.SerializeStandard(serial)
			End If
			json.WriteSettings(settings, serial)
		End If
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

		'Dim l As List(Of String) = database.SelectRequest(Of String)(efSelect.Text, AddressOf Feed)
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

	Private Function Feed(reader As OdbcDataReader) As String
		Return CDatabase.ItemValue(Of String)(reader, "IP")
	End Function

	Private Sub FTestCommon_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
		SaveSettings()
	End Sub

	Private Sub pbSaveSettings_Click(sender As Object, e As EventArgs) Handles pbSaveSettings.Click
		SaveSettings()
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

	Class C
		Public Property A As Integer = 3
		Public Property B As Integer = 3
		Public Property C As Integer = 3
	End Class

	Class B
		Public Property A As Integer = 2
		Public Property B As Integer = 2
		Public Property C As Integer = 2
		Public Property CC As C = New C
	End Class

	Class A
		Public Property A As Integer = 1
		Public Property B As Integer = 1
		Public Property C As Integer = 1
		Public Property CB As B = New B
	End Class

	Class X
		Public Property CA As A = New A
	End Class

	Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
		'Dim a As X = New X
		'Dim xml As XmlDocument = JsonConvert.DeserializeXmlNode(JsonConvert.SerializeObject(a))
		'Dim json As String = CJsonConverter.XMLToJson(xml.InnerXml)
		'Dim xmlj As String = CJsonConverter.JsonToXML(json, "CA")
		'xmlj = CJsonConverter.JsonToXML(json, "CB")
		'xmlj = CJsonConverter.JsonToXML(json, "CC")

		'*****
		'this function is only for use within special conditions and dedicated telco
		'*****
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
			.ReceiveTimeout = CStreamSettings.NO_TIMEOUT,
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

	Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
		myThread = New CThread
		myThread.Start(AddressOf ThreadFunction, New CThreadData With {.EventToSignal = myEvent, .OnTerminates = AddressOf ThreadHasEnded}, Nothing, Nothing, True)
	End Sub

	Private Sub UIProcessing(activity As UIActivity)
		Select Case activity.Evt
			Case UIActivityEnum.message
				lblMessage.Text = activity.Message
		End Select
	End Sub

	Private Function ThreadFunction(data As CThread, o As Object)
		Win32UIActivity.AddActivity(AddressOf UIProcessing, New UIActivity With {.Evt = UIActivityEnum.message, .Message = "Hello world", .Ctrl = Button2})
		Thread.Sleep(5000)
		Return ThreadResult.OK
	End Function

	Private Sub ThreadHasEnded(id As Integer, name As String, uniqueId As Int16, result As Integer)
		Win32UIActivity.AddActivity(AddressOf UIProcessing, New UIActivity With {.Evt = UIActivityEnum.message, .Message = "This is the end", .Ctrl = Button2})
	End Sub

	Private Sub ONE(s As String)
		Dim b As Byte
		For i As Integer = 0 To s.Length - 1
			b = CMisc.OneHexToBin(s(i))
		Next
	End Sub

	Private Sub TWO(s As String)
		Dim b As Byte
		For i As Integer = 0 To s.Length - 1 Step 2
			b = CMisc.TwoHexToBin(s.Substring(i, 2))
		Next
	End Sub
	Private Sub XXX(s As String)
		Dim d As Decimal
		Dim l As Long
		Dim b As Double
		Dim i As Integer
		Dim h As Short
		Try
			d = CMisc.HexToDecimal(s)
			b = CMisc.HexToDouble(s)
			l = CMisc.HexToLong(s)
			i = CMisc.HexToInt(s)
			h = CMisc.HexToShort(s)
		Catch ex As Exception
			s = ex.Message
		End Try
	End Sub

	Private Sub Button3_Click(sender As Object, e As EventArgs) Handles pbHex.Click
		XXX("000S")
		XXX("0000")
		XXX("05")
		XXX("9F36")
		XXX("9H36")
		XXX("FFFFFFFF")
		XXX("FFFFFFFFFFFFFFFF")
		XXX("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF")

		Dim d As String
		d = CMisc.ValueToHex(40758)
		d = CMisc.ValueToHex(5434684354, 13)
		d = CMisc.ValueToHex(5434684354, 13, True)
		d = CMisc.ValueToHex(5434684354, 0)
		d = CMisc.ValueToHex(5434684354, 0, True)
		Dim i1 As Long = &HFFFFFFFF
		Dim i2 As Decimal = 5434684354738999999
		d = CMisc.ValueToHex(i2 * i1)
	End Sub

	Public Enum AAA
		A
		B
		C
		D
	End Enum

	Class AB
		Property AB1 As String
		Property AB2 As Integer
	End Class

	Private Sub pbOther_Click(sender As Object, e As EventArgs) Handles pbOther.Click

		Dim cs As New CStreamClientSettings With {.IP = "192.168.0.111"}

		'Dim cs As New CSafeList(Of A)()

		Dim i As Integer = 65532
		Dim st As String = CMisc.AsHexString(CMisc.SetBytesFromIntegralTypeValue(i))
		st = CMisc.AsHexString(CMisc.SetBytesFromIntegralTypeValue(i, True))
		i = 56
		st = CMisc.AsHexString(CMisc.SetBytesFromIntegralTypeValue(i))
		st = CMisc.AsHexString(CMisc.SetBytesFromIntegralTypeValue(i, True))
		Dim s As Short = 32767
		st = CMisc.AsHexString(CMisc.SetBytesFromIntegralTypeValue(s))
		st = CMisc.AsHexString(CMisc.SetBytesFromIntegralTypeValue(s, True))
		s = 56
		st = CMisc.AsHexString(CMisc.SetBytesFromIntegralTypeValue(s))
		st = CMisc.AsHexString(CMisc.SetBytesFromIntegralTypeValue(s, True))
		Dim l As Long = -65532
		st = CMisc.AsHexString(CMisc.SetBytesFromIntegralTypeValue(l))
		st = CMisc.AsHexString(CMisc.SetBytesFromIntegralTypeValue(l, True))
		l = 567
		st = CMisc.AsHexString(CMisc.SetBytesFromIntegralTypeValue(l))
		st = CMisc.AsHexString(CMisc.SetBytesFromIntegralTypeValue(l, True))
		l = 1
		st = CMisc.AsHexString(CMisc.SetBytesFromIntegralTypeValue(l))
		st = CMisc.AsHexString(CMisc.SetBytesFromIntegralTypeValue(l, True))
		l = 10
		st = CMisc.AsHexString(CMisc.SetBytesFromIntegralTypeValue(l))
		st = CMisc.AsHexString(CMisc.SetBytesFromIntegralTypeValue(l, True))
		l = 100
		st = CMisc.AsHexString(CMisc.SetBytesFromIntegralTypeValue(l))
		st = CMisc.AsHexString(CMisc.SetBytesFromIntegralTypeValue(l, True))
		l = 1000
		st = CMisc.AsHexString(CMisc.SetBytesFromIntegralTypeValue(l))
		st = CMisc.AsHexString(CMisc.SetBytesFromIntegralTypeValue(l, True))
		l = 10000
		st = CMisc.AsHexString(CMisc.SetBytesFromIntegralTypeValue(l))
		st = CMisc.AsHexString(CMisc.SetBytesFromIntegralTypeValue(l, True))
		l = 1000000
		st = CMisc.AsHexString(CMisc.SetBytesFromIntegralTypeValue(l))
		st = CMisc.AsHexString(CMisc.SetBytesFromIntegralTypeValue(l, True))


		Dim z As New CStreamServerSettings
		Dim f As Boolean = CMisc.IsEnumValue(GetType(AAA), AAA.C)
		Dim aqw As AAA = CMisc.GetEnumValue(GetType(AAA), AAA.C.ToString)
		Dim aqws As String = CMisc.GetEnumName(GetType(AAA), AAA.C)

		Dim ip As String = CStream.Localhost(True)
		ip = CStream.Localhost(False)

		Dim min, max As Integer
		min = 0
		max = 66
		CMisc.AdjustMinMax(min, max, 1, 255)
		CMisc.AdjustMinMax(min, max, 1, 10)
		min = 100
		max = 6
		CMisc.AdjustMinMax(min, max, 1, 10)
		min = 100
		max = 6
		CMisc.AdjustMinMax(min, max, 1, 5875)
		min = 1 - 4
		max = &HFF
		CMisc.AdjustMinMax(min, max, 1, 255)
		min = 100
		max = 0
		CMisc.AdjustMinMax(min, max, 1, 5875)

		min = 100
		max = -6
		CMisc.AdjustMinMax(min, max, 1, 10)
		min = 100
		max = -6
		CMisc.AdjustMinMax0N(min, max, 10)
		min = 100
		max = -6
		CMisc.AdjustMinMax1N(min, max, 10)



		min = 100
		Dim ok As Boolean = True
	End Sub

End Class

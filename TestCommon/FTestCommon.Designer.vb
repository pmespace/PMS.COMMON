<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FTestCommon
	Inherits System.Windows.Forms.Form

	'Form overrides dispose to clean up the component list.
	<System.Diagnostics.DebuggerNonUserCode()> _
	Protected Overrides Sub Dispose(ByVal disposing As Boolean)
		Try
			If disposing AndAlso components IsNot Nothing Then
				components.Dispose()
			End If
		Finally
			MyBase.Dispose(disposing)
		End Try
	End Sub

	'Required by the Windows Form Designer
	Private components As System.ComponentModel.IContainer

	'NOTE: The following procedure is required by the Windows Form Designer
	'It can be modified using the Windows Form Designer.  
	'Do not modify it using the code editor.
	<System.Diagnostics.DebuggerStepThrough()> _
	Private Sub InitializeComponent()
		Me.components = New System.ComponentModel.Container()
		Me.pnlMain = New System.Windows.Forms.TableLayoutPanel()
		Me.TableLayoutPanel1 = New System.Windows.Forms.TableLayoutPanel()
		Me.pbSaveSettings = New System.Windows.Forms.Button()
		Me.pbClose = New System.Windows.Forms.Button()
		Me.Button1 = New System.Windows.Forms.Button()
		Me.lblResult = New System.Windows.Forms.Label()
		Me.TabControl1 = New System.Windows.Forms.TabControl()
		Me.databasePage = New System.Windows.Forms.TabPage()
		Me.TableLayoutPanel2 = New System.Windows.Forms.TableLayoutPanel()
		Me.pnlCommands = New System.Windows.Forms.TableLayoutPanel()
		Me.pnlConnection = New System.Windows.Forms.TableLayoutPanel()
		Me.Label1 = New System.Windows.Forms.Label()
		Me.efConnectionString = New System.Windows.Forms.TextBox()
		Me.pbTestConnectionString = New System.Windows.Forms.Button()
		Me.pnlSQL = New System.Windows.Forms.TableLayoutPanel()
		Me.Label4 = New System.Windows.Forms.Label()
		Me.Label2 = New System.Windows.Forms.Label()
		Me.Label3 = New System.Windows.Forms.Label()
		Me.efAnyRequest = New System.Windows.Forms.TextBox()
		Me.efSelect = New System.Windows.Forms.TextBox()
		Me.pbAnyRequest = New System.Windows.Forms.Button()
		Me.pbSelect = New System.Windows.Forms.Button()
		Me.lblSelectRes = New System.Windows.Forms.Label()
		Me.TableLayoutPanel3 = New System.Windows.Forms.TableLayoutPanel()
		Me.lblSQLRes = New System.Windows.Forms.Label()
		Me.lblSQLNbRows = New System.Windows.Forms.Label()
		Me.efTableName = New System.Windows.Forms.TextBox()
		Me.pbTable = New System.Windows.Forms.Button()
		Me.TableLayoutPanel4 = New System.Windows.Forms.TableLayoutPanel()
		Me.lblTableNbRows = New System.Windows.Forms.Label()
		Me.pbNbRows = New System.Windows.Forms.Button()
		Me.FlowLayoutPanel1 = New System.Windows.Forms.FlowLayoutPanel()
		Me.Button2 = New System.Windows.Forms.Button()
		Me.lblMessage = New System.Windows.Forms.Label()
		Me.Button3 = New System.Windows.Forms.Button()
		Me.DataGridView1 = New System.Windows.Forms.DataGridView()
		Me.ToolTip1 = New System.Windows.Forms.ToolTip(Me.components)
		Me.pnlMain.SuspendLayout()
		Me.TableLayoutPanel1.SuspendLayout()
		Me.TabControl1.SuspendLayout()
		Me.databasePage.SuspendLayout()
		Me.TableLayoutPanel2.SuspendLayout()
		Me.pnlCommands.SuspendLayout()
		Me.pnlConnection.SuspendLayout()
		Me.pnlSQL.SuspendLayout()
		Me.TableLayoutPanel3.SuspendLayout()
		Me.TableLayoutPanel4.SuspendLayout()
		Me.FlowLayoutPanel1.SuspendLayout()
		CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).BeginInit()
		Me.SuspendLayout()
		'
		'pnlMain
		'
		Me.pnlMain.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
			Or System.Windows.Forms.AnchorStyles.Left) _
			Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.pnlMain.AutoSize = True
		Me.pnlMain.ColumnCount = 1
		Me.pnlMain.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
		Me.pnlMain.Controls.Add(Me.TableLayoutPanel1, 0, 1)
		Me.pnlMain.Controls.Add(Me.TabControl1, 0, 0)
		Me.pnlMain.Location = New System.Drawing.Point(12, 12)
		Me.pnlMain.Name = "pnlMain"
		Me.pnlMain.RowCount = 2
		Me.pnlMain.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
		Me.pnlMain.RowStyles.Add(New System.Windows.Forms.RowStyle())
		Me.pnlMain.Size = New System.Drawing.Size(746, 481)
		Me.pnlMain.TabIndex = 0
		'
		'TableLayoutPanel1
		'
		Me.TableLayoutPanel1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
			Or System.Windows.Forms.AnchorStyles.Left) _
			Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.TableLayoutPanel1.AutoSize = True
		Me.TableLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
		Me.TableLayoutPanel1.ColumnCount = 4
		Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle())
		Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
		Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle())
		Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle())
		Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
		Me.TableLayoutPanel1.Controls.Add(Me.pbSaveSettings, 2, 0)
		Me.TableLayoutPanel1.Controls.Add(Me.pbClose, 3, 0)
		Me.TableLayoutPanel1.Controls.Add(Me.Button1, 0, 0)
		Me.TableLayoutPanel1.Controls.Add(Me.lblResult, 1, 0)
		Me.TableLayoutPanel1.Location = New System.Drawing.Point(3, 449)
		Me.TableLayoutPanel1.Name = "TableLayoutPanel1"
		Me.TableLayoutPanel1.RowCount = 1
		Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle())
		Me.TableLayoutPanel1.Size = New System.Drawing.Size(740, 29)
		Me.TableLayoutPanel1.TabIndex = 0
		'
		'pbSaveSettings
		'
		Me.pbSaveSettings.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.pbSaveSettings.AutoSize = True
		Me.pbSaveSettings.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
		Me.pbSaveSettings.Location = New System.Drawing.Point(607, 3)
		Me.pbSaveSettings.Name = "pbSaveSettings"
		Me.pbSaveSettings.Size = New System.Drawing.Size(81, 23)
		Me.pbSaveSettings.TabIndex = 0
		Me.pbSaveSettings.Text = "&Save settings"
		Me.pbSaveSettings.UseVisualStyleBackColor = True
		'
		'pbClose
		'
		Me.pbClose.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.pbClose.AutoSize = True
		Me.pbClose.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
		Me.pbClose.DialogResult = System.Windows.Forms.DialogResult.Cancel
		Me.pbClose.Location = New System.Drawing.Point(694, 3)
		Me.pbClose.Name = "pbClose"
		Me.pbClose.Size = New System.Drawing.Size(43, 23)
		Me.pbClose.TabIndex = 1
		Me.pbClose.Text = "&Close"
		Me.pbClose.UseVisualStyleBackColor = True
		'
		'Button1
		'
		Me.Button1.AutoSize = True
		Me.Button1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
		Me.Button1.Location = New System.Drawing.Point(3, 3)
		Me.Button1.Name = "Button1"
		Me.Button1.Size = New System.Drawing.Size(106, 23)
		Me.Button1.TabIndex = 2
		Me.Button1.Text = "Try pre-connection"
		Me.Button1.UseVisualStyleBackColor = True
		'
		'lblResult
		'
		Me.lblResult.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.lblResult.AutoSize = True
		Me.lblResult.Location = New System.Drawing.Point(115, 8)
		Me.lblResult.Name = "lblResult"
		Me.lblResult.Size = New System.Drawing.Size(486, 13)
		Me.lblResult.TabIndex = 3
		Me.lblResult.Text = "..."
		'
		'TabControl1
		'
		Me.TabControl1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
			Or System.Windows.Forms.AnchorStyles.Left) _
			Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.TabControl1.Controls.Add(Me.databasePage)
		Me.TabControl1.Location = New System.Drawing.Point(3, 3)
		Me.TabControl1.Name = "TabControl1"
		Me.TabControl1.SelectedIndex = 0
		Me.TabControl1.Size = New System.Drawing.Size(740, 440)
		Me.TabControl1.TabIndex = 2
		'
		'databasePage
		'
		Me.databasePage.Controls.Add(Me.TableLayoutPanel2)
		Me.databasePage.Location = New System.Drawing.Point(4, 22)
		Me.databasePage.Name = "databasePage"
		Me.databasePage.Padding = New System.Windows.Forms.Padding(3)
		Me.databasePage.Size = New System.Drawing.Size(732, 414)
		Me.databasePage.TabIndex = 0
		Me.databasePage.Text = "Database"
		Me.databasePage.UseVisualStyleBackColor = True
		'
		'TableLayoutPanel2
		'
		Me.TableLayoutPanel2.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
			Or System.Windows.Forms.AnchorStyles.Left) _
			Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.TableLayoutPanel2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
		Me.TableLayoutPanel2.ColumnCount = 1
		Me.TableLayoutPanel2.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
		Me.TableLayoutPanel2.Controls.Add(Me.pnlCommands, 0, 0)
		Me.TableLayoutPanel2.Controls.Add(Me.DataGridView1, 0, 1)
		Me.TableLayoutPanel2.Location = New System.Drawing.Point(6, 6)
		Me.TableLayoutPanel2.Name = "TableLayoutPanel2"
		Me.TableLayoutPanel2.RowCount = 2
		Me.TableLayoutPanel2.RowStyles.Add(New System.Windows.Forms.RowStyle())
		Me.TableLayoutPanel2.RowStyles.Add(New System.Windows.Forms.RowStyle())
		Me.TableLayoutPanel2.Size = New System.Drawing.Size(720, 402)
		Me.TableLayoutPanel2.TabIndex = 1
		'
		'pnlCommands
		'
		Me.pnlCommands.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
			Or System.Windows.Forms.AnchorStyles.Left) _
			Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.pnlCommands.AutoSize = True
		Me.pnlCommands.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
		Me.pnlCommands.ColumnCount = 1
		Me.pnlCommands.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
		Me.pnlCommands.Controls.Add(Me.pnlConnection, 0, 0)
		Me.pnlCommands.Controls.Add(Me.pnlSQL, 0, 1)
		Me.pnlCommands.Controls.Add(Me.FlowLayoutPanel1, 0, 2)
		Me.pnlCommands.Location = New System.Drawing.Point(3, 3)
		Me.pnlCommands.Name = "pnlCommands"
		Me.pnlCommands.RowCount = 3
		Me.pnlCommands.RowStyles.Add(New System.Windows.Forms.RowStyle())
		Me.pnlCommands.RowStyles.Add(New System.Windows.Forms.RowStyle())
		Me.pnlCommands.RowStyles.Add(New System.Windows.Forms.RowStyle())
		Me.pnlCommands.Size = New System.Drawing.Size(714, 169)
		Me.pnlCommands.TabIndex = 2
		'
		'pnlConnection
		'
		Me.pnlConnection.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
			Or System.Windows.Forms.AnchorStyles.Left) _
			Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.pnlConnection.AutoSize = True
		Me.pnlConnection.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
		Me.pnlConnection.ColumnCount = 3
		Me.pnlConnection.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle())
		Me.pnlConnection.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
		Me.pnlConnection.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle())
		Me.pnlConnection.Controls.Add(Me.Label1, 0, 0)
		Me.pnlConnection.Controls.Add(Me.efConnectionString, 1, 0)
		Me.pnlConnection.Controls.Add(Me.pbTestConnectionString, 2, 0)
		Me.pnlConnection.Location = New System.Drawing.Point(3, 3)
		Me.pnlConnection.Name = "pnlConnection"
		Me.pnlConnection.RowCount = 1
		Me.pnlConnection.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
		Me.pnlConnection.Size = New System.Drawing.Size(708, 29)
		Me.pnlConnection.TabIndex = 0
		'
		'Label1
		'
		Me.Label1.Anchor = System.Windows.Forms.AnchorStyles.Right
		Me.Label1.AutoSize = True
		Me.Label1.Location = New System.Drawing.Point(3, 8)
		Me.Label1.Name = "Label1"
		Me.Label1.Size = New System.Drawing.Size(92, 13)
		Me.Label1.TabIndex = 0
		Me.Label1.Text = "Connection string:"
		'
		'efConnectionString
		'
		Me.efConnectionString.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.efConnectionString.Location = New System.Drawing.Point(101, 4)
		Me.efConnectionString.Name = "efConnectionString"
		Me.efConnectionString.Size = New System.Drawing.Size(523, 20)
		Me.efConnectionString.TabIndex = 0
		Me.ToolTip1.SetToolTip(Me.efConnectionString, "DSN=<odbc entry>")
		'
		'pbTestConnectionString
		'
		Me.pbTestConnectionString.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.pbTestConnectionString.Location = New System.Drawing.Point(630, 3)
		Me.pbTestConnectionString.Name = "pbTestConnectionString"
		Me.pbTestConnectionString.Size = New System.Drawing.Size(75, 23)
		Me.pbTestConnectionString.TabIndex = 1
		Me.pbTestConnectionString.Text = "&Test"
		Me.pbTestConnectionString.UseVisualStyleBackColor = True
		'
		'pnlSQL
		'
		Me.pnlSQL.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
			Or System.Windows.Forms.AnchorStyles.Left) _
			Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.pnlSQL.AutoScroll = True
		Me.pnlSQL.AutoSize = True
		Me.pnlSQL.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
		Me.pnlSQL.ColumnCount = 4
		Me.pnlSQL.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle())
		Me.pnlSQL.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
		Me.pnlSQL.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle())
		Me.pnlSQL.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle())
		Me.pnlSQL.Controls.Add(Me.Label4, 0, 2)
		Me.pnlSQL.Controls.Add(Me.Label2, 0, 1)
		Me.pnlSQL.Controls.Add(Me.Label3, 0, 0)
		Me.pnlSQL.Controls.Add(Me.efAnyRequest, 1, 1)
		Me.pnlSQL.Controls.Add(Me.efSelect, 1, 0)
		Me.pnlSQL.Controls.Add(Me.pbAnyRequest, 2, 1)
		Me.pnlSQL.Controls.Add(Me.pbSelect, 2, 0)
		Me.pnlSQL.Controls.Add(Me.lblSelectRes, 3, 0)
		Me.pnlSQL.Controls.Add(Me.TableLayoutPanel3, 3, 1)
		Me.pnlSQL.Controls.Add(Me.efTableName, 1, 2)
		Me.pnlSQL.Controls.Add(Me.pbTable, 2, 2)
		Me.pnlSQL.Controls.Add(Me.TableLayoutPanel4, 3, 2)
		Me.pnlSQL.Location = New System.Drawing.Point(3, 38)
		Me.pnlSQL.Name = "pnlSQL"
		Me.pnlSQL.RowCount = 3
		Me.pnlSQL.RowStyles.Add(New System.Windows.Forms.RowStyle())
		Me.pnlSQL.RowStyles.Add(New System.Windows.Forms.RowStyle())
		Me.pnlSQL.RowStyles.Add(New System.Windows.Forms.RowStyle())
		Me.pnlSQL.Size = New System.Drawing.Size(708, 93)
		Me.pnlSQL.TabIndex = 9
		'
		'Label4
		'
		Me.Label4.Anchor = System.Windows.Forms.AnchorStyles.Right
		Me.Label4.AutoSize = True
		Me.Label4.Location = New System.Drawing.Point(15, 69)
		Me.Label4.Name = "Label4"
		Me.Label4.Size = New System.Drawing.Size(66, 13)
		Me.Label4.TabIndex = 10
		Me.Label4.Text = "Table name:"
		'
		'Label2
		'
		Me.Label2.Anchor = System.Windows.Forms.AnchorStyles.Right
		Me.Label2.AutoSize = True
		Me.Label2.Location = New System.Drawing.Point(12, 37)
		Me.Label2.Name = "Label2"
		Me.Label2.Size = New System.Drawing.Size(69, 13)
		Me.Label2.TabIndex = 2
		Me.Label2.Text = "SQL request:"
		'
		'Label3
		'
		Me.Label3.Anchor = System.Windows.Forms.AnchorStyles.Right
		Me.Label3.AutoSize = True
		Me.Label3.Location = New System.Drawing.Point(3, 8)
		Me.Label3.Name = "Label3"
		Me.Label3.Size = New System.Drawing.Size(78, 13)
		Me.Label3.TabIndex = 6
		Me.Label3.Text = "Select request:"
		'
		'efAnyRequest
		'
		Me.efAnyRequest.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.efAnyRequest.Location = New System.Drawing.Point(87, 33)
		Me.efAnyRequest.Name = "efAnyRequest"
		Me.efAnyRequest.Size = New System.Drawing.Size(441, 20)
		Me.efAnyRequest.TabIndex = 2
		'
		'efSelect
		'
		Me.efSelect.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.efSelect.Location = New System.Drawing.Point(87, 4)
		Me.efSelect.Name = "efSelect"
		Me.efSelect.Size = New System.Drawing.Size(441, 20)
		Me.efSelect.TabIndex = 0
		'
		'pbAnyRequest
		'
		Me.pbAnyRequest.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.pbAnyRequest.Location = New System.Drawing.Point(534, 32)
		Me.pbAnyRequest.Name = "pbAnyRequest"
		Me.pbAnyRequest.Size = New System.Drawing.Size(75, 23)
		Me.pbAnyRequest.TabIndex = 3
		Me.pbAnyRequest.Text = "Start"
		Me.pbAnyRequest.UseVisualStyleBackColor = True
		'
		'pbSelect
		'
		Me.pbSelect.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.pbSelect.Location = New System.Drawing.Point(534, 3)
		Me.pbSelect.Name = "pbSelect"
		Me.pbSelect.Size = New System.Drawing.Size(75, 23)
		Me.pbSelect.TabIndex = 1
		Me.pbSelect.Text = "Start"
		Me.pbSelect.UseVisualStyleBackColor = True
		'
		'lblSelectRes
		'
		Me.lblSelectRes.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.lblSelectRes.AutoSize = True
		Me.lblSelectRes.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
		Me.lblSelectRes.Location = New System.Drawing.Point(615, 7)
		Me.lblSelectRes.Name = "lblSelectRes"
		Me.lblSelectRes.Size = New System.Drawing.Size(90, 15)
		Me.lblSelectRes.TabIndex = 9
		Me.lblSelectRes.Text = "Label4"
		Me.lblSelectRes.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		'
		'TableLayoutPanel3
		'
		Me.TableLayoutPanel3.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
			Or System.Windows.Forms.AnchorStyles.Left) _
			Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.TableLayoutPanel3.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
		Me.TableLayoutPanel3.ColumnCount = 2
		Me.TableLayoutPanel3.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
		Me.TableLayoutPanel3.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
		Me.TableLayoutPanel3.Controls.Add(Me.lblSQLRes, 0, 0)
		Me.TableLayoutPanel3.Controls.Add(Me.lblSQLNbRows, 1, 0)
		Me.TableLayoutPanel3.Location = New System.Drawing.Point(615, 32)
		Me.TableLayoutPanel3.Name = "TableLayoutPanel3"
		Me.TableLayoutPanel3.RowCount = 1
		Me.TableLayoutPanel3.RowStyles.Add(New System.Windows.Forms.RowStyle())
		Me.TableLayoutPanel3.Size = New System.Drawing.Size(90, 23)
		Me.TableLayoutPanel3.TabIndex = 8
		'
		'lblSQLRes
		'
		Me.lblSQLRes.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.lblSQLRes.AutoSize = True
		Me.lblSQLRes.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
		Me.lblSQLRes.Location = New System.Drawing.Point(3, 4)
		Me.lblSQLRes.Name = "lblSQLRes"
		Me.lblSQLRes.Size = New System.Drawing.Size(39, 15)
		Me.lblSQLRes.TabIndex = 0
		Me.lblSQLRes.Text = "res"
		Me.lblSQLRes.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		'
		'lblSQLNbRows
		'
		Me.lblSQLNbRows.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.lblSQLNbRows.AutoSize = True
		Me.lblSQLNbRows.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
		Me.lblSQLNbRows.Location = New System.Drawing.Point(48, 4)
		Me.lblSQLNbRows.Name = "lblSQLNbRows"
		Me.lblSQLNbRows.Size = New System.Drawing.Size(39, 15)
		Me.lblSQLNbRows.TabIndex = 1
		Me.lblSQLNbRows.Text = "nb"
		Me.lblSQLNbRows.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		'
		'efTableName
		'
		Me.efTableName.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.efTableName.Location = New System.Drawing.Point(87, 65)
		Me.efTableName.Name = "efTableName"
		Me.efTableName.Size = New System.Drawing.Size(441, 20)
		Me.efTableName.TabIndex = 11
		'
		'pbTable
		'
		Me.pbTable.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.pbTable.Location = New System.Drawing.Point(534, 64)
		Me.pbTable.Name = "pbTable"
		Me.pbTable.Size = New System.Drawing.Size(75, 23)
		Me.pbTable.TabIndex = 12
		Me.pbTable.Text = "Display"
		Me.pbTable.UseVisualStyleBackColor = True
		'
		'TableLayoutPanel4
		'
		Me.TableLayoutPanel4.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
			Or System.Windows.Forms.AnchorStyles.Left) _
			Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.TableLayoutPanel4.AutoSize = True
		Me.TableLayoutPanel4.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
		Me.TableLayoutPanel4.ColumnCount = 2
		Me.TableLayoutPanel4.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
		Me.TableLayoutPanel4.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
		Me.TableLayoutPanel4.Controls.Add(Me.lblTableNbRows, 1, 0)
		Me.TableLayoutPanel4.Controls.Add(Me.pbNbRows, 0, 0)
		Me.TableLayoutPanel4.Location = New System.Drawing.Point(615, 61)
		Me.TableLayoutPanel4.Name = "TableLayoutPanel4"
		Me.TableLayoutPanel4.RowCount = 1
		Me.TableLayoutPanel4.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
		Me.TableLayoutPanel4.Size = New System.Drawing.Size(90, 29)
		Me.TableLayoutPanel4.TabIndex = 13
		'
		'lblTableNbRows
		'
		Me.lblTableNbRows.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.lblTableNbRows.AutoSize = True
		Me.lblTableNbRows.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
		Me.lblTableNbRows.Location = New System.Drawing.Point(48, 7)
		Me.lblTableNbRows.Name = "lblTableNbRows"
		Me.lblTableNbRows.Size = New System.Drawing.Size(39, 15)
		Me.lblTableNbRows.TabIndex = 14
		Me.lblTableNbRows.Text = "nb"
		Me.lblTableNbRows.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		'
		'pbNbRows
		'
		Me.pbNbRows.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.pbNbRows.AutoSize = True
		Me.pbNbRows.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
		Me.pbNbRows.Location = New System.Drawing.Point(3, 3)
		Me.pbNbRows.Name = "pbNbRows"
		Me.pbNbRows.Size = New System.Drawing.Size(39, 23)
		Me.pbNbRows.TabIndex = 13
		Me.pbNbRows.Text = "Nb"
		Me.pbNbRows.UseVisualStyleBackColor = True
		'
		'FlowLayoutPanel1
		'
		Me.FlowLayoutPanel1.AutoSize = True
		Me.FlowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
		Me.FlowLayoutPanel1.Controls.Add(Me.Button2)
		Me.FlowLayoutPanel1.Controls.Add(Me.lblMessage)
		Me.FlowLayoutPanel1.Controls.Add(Me.Button3)
		Me.FlowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill
		Me.FlowLayoutPanel1.Location = New System.Drawing.Point(3, 137)
		Me.FlowLayoutPanel1.Name = "FlowLayoutPanel1"
		Me.FlowLayoutPanel1.Size = New System.Drawing.Size(708, 29)
		Me.FlowLayoutPanel1.TabIndex = 10
		'
		'Button2
		'
		Me.Button2.Location = New System.Drawing.Point(3, 3)
		Me.Button2.Name = "Button2"
		Me.Button2.Size = New System.Drawing.Size(75, 23)
		Me.Button2.TabIndex = 0
		Me.Button2.Text = "Thread"
		Me.Button2.UseVisualStyleBackColor = True
		'
		'lblMessage
		'
		Me.lblMessage.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.lblMessage.AutoSize = True
		Me.lblMessage.Location = New System.Drawing.Point(84, 8)
		Me.lblMessage.Name = "lblMessage"
		Me.lblMessage.Size = New System.Drawing.Size(39, 13)
		Me.lblMessage.TabIndex = 1
		Me.lblMessage.Text = "Label5"
		'
		'Button3
		'
		Me.Button3.Location = New System.Drawing.Point(129, 3)
		Me.Button3.Name = "Button3"
		Me.Button3.Size = New System.Drawing.Size(75, 23)
		Me.Button3.TabIndex = 2
		Me.Button3.Text = "Test HEX<>Value"
		Me.Button3.UseVisualStyleBackColor = True
		Me.Button3.Visible = False
		'
		'DataGridView1
		'
		Me.DataGridView1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
			Or System.Windows.Forms.AnchorStyles.Left) _
			Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.DataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
		Me.DataGridView1.Location = New System.Drawing.Point(3, 178)
		Me.DataGridView1.Name = "DataGridView1"
		Me.DataGridView1.Size = New System.Drawing.Size(714, 221)
		Me.DataGridView1.TabIndex = 0
		'
		'FTestCommon
		'
		Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
		Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
		Me.CancelButton = Me.pbClose
		Me.ClientSize = New System.Drawing.Size(770, 505)
		Me.Controls.Add(Me.pnlMain)
		Me.Name = "FTestCommon"
		Me.Text = "Test COMMON Services"
		Me.pnlMain.ResumeLayout(False)
		Me.pnlMain.PerformLayout()
		Me.TableLayoutPanel1.ResumeLayout(False)
		Me.TableLayoutPanel1.PerformLayout()
		Me.TabControl1.ResumeLayout(False)
		Me.databasePage.ResumeLayout(False)
		Me.TableLayoutPanel2.ResumeLayout(False)
		Me.TableLayoutPanel2.PerformLayout()
		Me.pnlCommands.ResumeLayout(False)
		Me.pnlCommands.PerformLayout()
		Me.pnlConnection.ResumeLayout(False)
		Me.pnlConnection.PerformLayout()
		Me.pnlSQL.ResumeLayout(False)
		Me.pnlSQL.PerformLayout()
		Me.TableLayoutPanel3.ResumeLayout(False)
		Me.TableLayoutPanel3.PerformLayout()
		Me.TableLayoutPanel4.ResumeLayout(False)
		Me.TableLayoutPanel4.PerformLayout()
		Me.FlowLayoutPanel1.ResumeLayout(False)
		Me.FlowLayoutPanel1.PerformLayout()
		CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).EndInit()
		Me.ResumeLayout(False)
		Me.PerformLayout()

	End Sub

	Friend WithEvents pnlMain As TableLayoutPanel
	Friend WithEvents TableLayoutPanel1 As TableLayoutPanel
	Friend WithEvents TabControl1 As TabControl
	Friend WithEvents databasePage As TabPage
	Friend WithEvents TableLayoutPanel2 As TableLayoutPanel
	Friend WithEvents pnlCommands As TableLayoutPanel
	Friend WithEvents efConnectionString As TextBox
	Friend WithEvents Label1 As Label
	Friend WithEvents Label2 As Label
	Friend WithEvents efAnyRequest As TextBox
	Friend WithEvents pbTestConnectionString As Button
	Friend WithEvents pbAnyRequest As Button
	Friend WithEvents pbSelect As Button
	Friend WithEvents Label3 As Label
	Friend WithEvents efSelect As TextBox
	Friend WithEvents pnlConnection As TableLayoutPanel
	Friend WithEvents pnlSQL As TableLayoutPanel
	Friend WithEvents lblSelectRes As Label
	Friend WithEvents TableLayoutPanel3 As TableLayoutPanel
	Friend WithEvents lblSQLRes As Label
	Friend WithEvents lblSQLNbRows As Label
	Friend WithEvents DataGridView1 As DataGridView
	Friend WithEvents pbSaveSettings As Button
	Friend WithEvents pbClose As Button
	Friend WithEvents Label4 As Label
	Friend WithEvents efTableName As TextBox
	Friend WithEvents pbTable As Button
	Friend WithEvents Button1 As Button
	Friend WithEvents lblResult As Label
	Friend WithEvents pbNbRows As Button
	Friend WithEvents TableLayoutPanel4 As TableLayoutPanel
	Friend WithEvents lblTableNbRows As Label
	Friend WithEvents FlowLayoutPanel1 As FlowLayoutPanel
	Friend WithEvents Button2 As Button
	Friend WithEvents lblMessage As Label
	Friend WithEvents ToolTip1 As ToolTip
	Friend WithEvents Button3 As Button
End Class

Public Module WMI

	'Public Enum EnumMotherBoardInfo
	'	_begin = 0
	'	Depth
	'	Description
	'	Height
	'	HostingBoard
	'	HotSwappable
	'	Manufacturer
	'	Model
	'	Name
	'	OtherIdentifyingInformation
	'	PartNumber
	'	PoweredOn
	'	Product
	'	Removable
	'	Replaceable
	'	RequirementsDescription
	'	RequiresDaughterboard
	'	SerialNumber
	'	SKU
	'	SlotLayout
	'	SpecialRequirements
	'	Tag
	'	Version
	'	Weight
	'	Width
	'	_end
	'End Enum
	Public Function MotherBoardsSerialNumber() As String
		Dim result As String = String.Empty
		Try
			Dim computer As String = "."
			Dim wmi As Object = GetObject("winmgmts:" & "{impersonationLevel=impersonate}!\\" & computer & ".\root\cimv2")
			Dim boards As Object = wmi.ExecQuery("select * from Win32_BaseBoard")
			For Each board As Object In boards
				'Select Case info
				'	Case Depth
				'		result &= board.Depth
				'	Case Description
				'		result = board.Description
				'	Case Height
				'		result = board.Height
				'	Case HostingBoard
				'		result = board.HostingBoard
				'	Case HotSwappable
				'		result = board.HotSwappable
				'	Case Manufacturer
				'		result = board.Manufacturer
				'	Case Model
				'		result = board.Model
				'	Case Name
				'		result = board.Name
				'	Case OtherIdentifyingInformation
				'		result = board.OtherIdentifyingInfo
				'	Case PartNumber
				'		result = board.PartNumber
				'	Case PoweredOn
				'		result = board.PoweredOn
				'	Case Product
				'		result = board.Product
				'	Case Removable
				'		result = board.Removable
				'	Case Replaceable
				'		result = board.Replaceable
				'	Case RequirementsDescription
				'		result = board.RequirementsDescription
				'	Case RequiresDaughterboard
				'		result = board.RequiresDaughterBoard
				'	Case SerialNumber
				result = board.SerialNumber
				'	Case SKU
				'		result = board.SKU
				'	Case SlotLayout
				'		result = board.SlotLayout
				'	Case SpecialRequirements
				'		result = board.SpecialRequirements
				'	Case Tag
				'		result = board.Tag
				'	Case Version
				'		result = board.Version
				'	Case Weight
				'		result = board.Weight
				'	Case WidthCase
				'		result = board.Width
				'End Select
			Next board
		Catch ex As Exception
		End Try
		Return result
	End Function
	Public Function CpusSerialNumber() As String
		Dim result As String = ""
		Try
			Dim computer As String = "."
			Dim wmi As Object = GetObject("winmgmts:" & "{impersonationLevel=impersonate}!\\" & computer & "\root\cimv2")
			Dim cpus As Object = wmi.ExecQuery("Select * from " & "Win32_Processor")
			For Each cpu As Object In cpus
				result &= cpu.ProcessorId
			Next cpu
		Catch ex As Exception
		End Try
		Return result
	End Function

End Module

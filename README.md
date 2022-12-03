# PMS.COMMON
Shared tools by PMS including:
- CLog: log file management
- CStream,...: network streams management
  It includes low level functions to manage streams over sockets (either network or SSL)
  It also includes a standard CStreamServer allowing to easily implementa a socket server receiving messages and using callbacks to inform a caller a message is to process
- CJson: a lightweight json files management (uses NewtoSoft.Json)
  It allows to easily manage .JSON files for simple and standard use.
- CThread: tools to start threads and be warned when they stop
- CMisc: a set of various tools
- CDatabase: ODBC database management wrapper
.NET Framework only
- Functions allowing to easily manage UI activity frim within threads
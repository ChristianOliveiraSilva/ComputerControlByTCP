$client = New-Object System.Net.Sockets.TcpClient("127.0.0.1",5000)
$stream = $client.GetStream()
$writer = New-Object System.IO.StreamWriter($stream)
$writer.AutoFlush = $true
$writer.WriteLine("troque_esse_seguro_token:notepad")
$reader = New-Object System.IO.StreamReader($stream)
$resp = $reader.ReadLine()
Write-Output $resp
$client.Close()
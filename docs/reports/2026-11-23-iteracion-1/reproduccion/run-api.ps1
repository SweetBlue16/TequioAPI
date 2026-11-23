$env:ASPNETCORE_ENVIRONMENT='Development'
$env:ConnectionStrings__TequioDatabase="Server=.\SQLEXPRESS;Database=$env:TEQUIO_TEST_DATABASE;Integrated Security=true;TrustServerCertificate=true"
$env:TEQUIO_JWT_SIGNING_KEY=[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
$env:MasterAdmin__Email='test-master@tequio.invalid'
$env:MasterAdmin__Password=$env:TEQUIO_TEST_MASTER_PASSWORD
$env:Email__Host='localhost'
$env:Email__Port='1025'
$env:Email__Username='test-smtp@tequio.invalid'
$env:Email__Password=$env:TEQUIO_TEST_SMTP_PASSWORD
$env:Email__FromAddress=$env:Email__Username
$env:Email__FromName='Tequio Pruebas'
$env:Email__UseStartTls='true'
$env:Logging__LogLevel__Default='Warning'
$env:Logging__LogLevel__Microsoft='Warning'
dotnet run --project backend/Tequio.Api --no-launch-profile --no-build --urls http://localhost:5168

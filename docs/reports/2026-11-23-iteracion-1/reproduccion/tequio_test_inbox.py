import os
import re
import ssl
import sys
import time
from pathlib import Path
from email import policy
from email.parser import BytesParser

stage = Path(__file__).parent
sys.path.insert(0, str(stage / 'python'))
from aiosmtpd.controller import Controller
from aiosmtpd.smtp import AuthResult

class TestInbox:
    async def handle_DATA(self, server, session, envelope):
        address = envelope.rcpt_tos[0]
        if not re.fullmatch(r'test-[a-z0-9-]+@tequio\.invalid', address):
            return '550 Only isolated test inboxes are accepted'
        message = BytesParser(policy=policy.default).parsebytes(envelope.original_content)
        text = message.get_body(preferencelist=('plain',)).get_content()
        code = re.search(r'\b[0-9]{6}\b', text)
        if code is None:
            return '554 Verification code missing'
        (stage / (address.split('@')[0] + '.otp')).write_text(code.group())
        return '250 Accepted into isolated test inbox'

def authenticate(server, session, envelope, mechanism, credentials):
    return AuthResult(success=credentials.login == b'test-smtp@tequio.invalid' and credentials.password == os.environ['TEQUIO_TEST_SMTP_PASSWORD'].encode(), handled=False)

context = ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER)
context.load_cert_chain(stage / 'smtp.pem', stage / 'smtp.key')
controller = Controller(TestInbox(), hostname='127.0.0.1', port=1025,
                        tls_context=context, require_starttls=True,
                        auth_required=True, authenticator=authenticate)
controller.start()
print('Mailbox STARTTLS listening on 127.0.0.1:1025', flush=True)
try:
    while True:
        time.sleep(1)
finally:
    controller.stop()

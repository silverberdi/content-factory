const fs = require('fs');
const path = require('path');

const rootEnvPath = path.resolve(__dirname, '../../../.env');
const devEnvPath = path.resolve(__dirname, '../src/environments/environment.development.ts');

if (fs.existsSync(rootEnvPath) && fs.existsSync(devEnvPath)) {
  const envContent = fs.readFileSync(rootEnvPath, 'utf8');
  const match = envContent.match(/^PRIMENG_LICENSE_KEY=(.*)$/m);
  if (match && match[1]) {
    const license = match[1].trim();
    let devContent = fs.readFileSync(devEnvPath, 'utf8');
    devContent = devContent.replace(/primengLicenseKey:\s*'.*?'/, `primengLicenseKey: '${license}'`);
    fs.writeFileSync(devEnvPath, devContent, 'utf8');
    console.log('[sync-env] Synchronized PRIMENG_LICENSE_KEY from root .env to environment.development.ts');
  }
}

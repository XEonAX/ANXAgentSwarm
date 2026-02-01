# Security Policy

## Supported Versions

We release patches for security vulnerabilities in the following versions:

| Version | Supported          |
| ------- | ------------------ |
| 1.x.x   | :white_check_mark: |
| < 1.0   | :x:                |

## Reporting a Vulnerability

We take the security of ANXAgentSwarm seriously. If you discover a security vulnerability, please follow these steps:

### 1. Do Not Disclose Publicly

Please **do not** create a public GitHub issue for security vulnerabilities. This helps protect users while we work on a fix.

### 2. Report Privately

Report vulnerabilities through one of these channels:

- **GitHub Security Advisories**: [Create a private security advisory](https://github.com/yourusername/ANXAgentSwarm/security/advisories/new)
- **Email**: security@anxagentswarm.example.com (replace with your actual security email)

### 3. Provide Details

When reporting, please include:

- **Description**: A clear description of the vulnerability
- **Steps to Reproduce**: Detailed steps to reproduce the issue
- **Impact**: The potential impact of the vulnerability
- **Affected Versions**: Which versions are affected
- **Suggested Fix**: If you have ideas for how to fix it

### 4. Response Timeline

We aim to respond to security reports within:

- **Initial Response**: 48 hours
- **Assessment**: 1 week
- **Fix (Critical)**: 72 hours after assessment
- **Fix (High)**: 1 week after assessment
- **Fix (Medium/Low)**: Next release cycle

## Security Measures

ANXAgentSwarm implements the following security measures:

### Input Validation
- All API inputs are validated and sanitized
- File operations are restricted to the workspace directory
- Path traversal attacks are prevented

### Authentication & Authorization
- Session-based access control
- No sensitive data stored in browser storage
- CORS configuration for API security

### Data Protection
- SQLite database with file-level access control
- No sensitive credentials stored in code
- Environment variables for configuration

### Network Security
- HTTPS recommended for production
- WebSocket connections secured with SignalR
- Rate limiting on API endpoints

### Dependency Security
- Dependabot enabled for automatic updates
- Regular security audits with npm audit and dotnet list vulnerabilities
- CodeQL static analysis on all PRs

## Security Best Practices for Deployment

When deploying ANXAgentSwarm:

1. **Use HTTPS**: Always use TLS/SSL in production
2. **Firewall**: Restrict access to required ports only
3. **Environment Variables**: Store secrets in environment variables
4. **Regular Updates**: Keep all dependencies updated
5. **Monitoring**: Enable logging and monitoring
6. **Backup**: Regularly backup the SQLite database
7. **Access Control**: Limit who can access the application

## Known Security Considerations

### LLM Prompt Injection
- The system uses AI personas that process user input
- Input sanitization is applied, but LLM systems can be unpredictable
- Monitor outputs for unexpected behavior

### File System Access
- Agents can create files in the workspace directory
- Access is restricted to prevent directory traversal
- Consider sandboxing for high-security environments

### Third-Party Dependencies
- The system relies on Ollama for LLM functionality
- Ensure Ollama is properly secured
- Review third-party package security regularly

## Updates and Patches

Security patches are released as soon as possible after a vulnerability is confirmed:

1. A fix is developed and tested
2. The patch is released to all supported versions
3. A security advisory is published on GitHub
4. Users are notified through GitHub notifications

## Acknowledgments

We appreciate security researchers who help keep ANXAgentSwarm secure. Contributors who responsibly disclose vulnerabilities will be acknowledged (with permission) in:

- Security advisories
- Release notes
- CHANGELOG.md

Thank you for helping keep ANXAgentSwarm and its users safe!

import http from 'k6/http';

/**
 * Authenticate using OAuth against Azure Active Directory
 * @function
 * @param  {string} tenantId - Directory ID in Azure
 * @param  {string} clientId - Application ID in Azure
 * @param  {string} clientSecret - Can be obtained from https://docs.microsoft.com/en-us/azure/storage/common/storage-auth-aad-app#create-a-client-secret
 * @param  {string} scope - Space-separated list of scopes (permissions) that are already given consent to by admin
 * @param  {string} resource - a resource ID (as string) 
 */
export function authenticateUsingAzure(tenantId, clientId, clientSecret, scope, resource) {
  let url;
  const requestBody = {
    client_id: clientId,
    client_secret: clientSecret,
    scope: scope,
  };

  if (typeof resource == 'string') {
    url = `https://login.microsoftonline.com/${tenantId}/oauth2/token`;
      requestBody['grant_type'] = 'client_credentials';
    requestBody['resource'] = resource;
  } else {
    throw 'resource should be either a string or an object containing username and password';
  }

  let response = http.post(url, requestBody);

  return response.json();
}

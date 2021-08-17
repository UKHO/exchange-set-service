import http from 'k6/http';
const config = JSON.parse(open('./config.json'));

export function GetESSApiResponse(endPoint, data, essToken) {
    var essUrl = `${config.Base_URL}/productData/${endPoint}`;
    let essResponse = http.post(essUrl, JSON.stringify(data),{ headers: { Authorization: `Bearer ${essToken}` , "Content-Type": "application/json" } });
    let jsonResponse = JSON.parse(essResponse.body);
    let batchStatusUrl = JSON.stringify(jsonResponse['_links']['exchangeSetBatchStatusUri']['href']);
    return batchStatusUrl;
};

export function GetFSSApiResponse(url, fssToken) {
     let fssResponse = http.get(url, { headers: { Authorization: `Bearer ${fssToken}` , "Content-Type": "application/json" } });
     var batchStatusResponse = JSON.parse(fssResponse.body)
     let fssCommitStatus = JSON.stringify(batchStatusResponse['status']);
    return fssCommitStatus;
};
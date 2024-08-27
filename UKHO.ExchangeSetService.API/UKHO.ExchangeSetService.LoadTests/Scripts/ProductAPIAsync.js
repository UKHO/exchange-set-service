import http from 'k6/http';
import { check } from 'k6';
import exec from 'k6/execution';

const config = JSON.parse(open('../config.json'));
const report = require('../Helper/MetricHelper.js');

var FSSBatchResponseTime;

/**
 * @param {Object} requestURL The set of Product IDs to create Exchange Set 
 * @param {Object} requestBody The set of Product IDs to create Exchange Set 
*/
export function resendRequest(requestURL, requestBody) {
    let essRes = http.post(requestURL, requestBody,
        { headers: { Authorization: `Bearer ${config.ESSToken}`, "Content-Type": "application/json" } });

    if (essRes.status == 401) {
        console.error("Bearer token expired!");
        exec.test.abort("Please update token, Execution Stopped!");
    }

    check(essRes, {
        'HTTP Response Code': (essRes) => essRes.status = 200
    });

    if (essRes.status == 200) {
        try {
            if (essRes.body != null && typeof essRes.json().fssBatchId === 'undefined') {
                console.log("Response:" + JSON.stringify(essRes.body, null, 2));
                console.log("Correlation-Id:" + essRes.headers['X-Correlation-Id']);
            }
            else if(essRes.body === null){
                console.log("Status Code:200, Response Body: null, Request:"+essRes.request.url)
            }
            else {
                console.log("Batch ID:" + JSON.stringify(essRes.json().fssBatchId), "File URL:" + JSON.stringify(essRes.json()._links.exchangeSetFileUri.href));
                console.log("Correlation-Id:" + essRes.headers['X-Correlation-Id']);
            }
  
        } catch (e) {
            console.log(essRes.body);
        }
    }

    if (essRes.status != 200) {
        console.error("URL:" + requestURL, "Body:" + requestBody);
        console.error("Response:" + JSON.stringify(requestBody, null, 2));
        console.error("Correlation-Id:" + essRes.headers['X-Correlation-Id']);
        console.error("Response Code:" + essRes.status);
    }
}

export async function getFSSApiResponse(url) {
    var urlPortion = url.split(config.FSSDetailURI); //Hard coded for environment
    let fssResponse = await http.asyncRequest('GET', `${config.FSS_URL}${urlPortion[1]}`, { headers: { Authorization: `Bearer ${config.FSSToken}`, "Content-Type": "application/json" } });
    FSSBatchResponseTime = FSSBatchResponseTime + fssResponse.timings.duration;
    var batchStatusResponse = JSON.parse(fssResponse.body);
    let fssCommitStatus = JSON.parse(JSON.stringify(batchStatusResponse['status']));
    return fssCommitStatus;
};

export async function getFSSApiDetailsResponse(url) {
    var urlPortion = url.split(config.FSSDetailURI); //Hard coded for environment
    let fssResponse = await http.asyncRequest('GET', `${config.FSS_URL}${urlPortion[1]}`, { headers: { Authorization: `Bearer ${config.FSSToken}`, "Content-Type": "application/json" } });
    let jsonResponse = JSON.parse(fssResponse.body);
    return jsonResponse;
};

export function replayGetRequest(url) {
    var group_duration = report.GetGroupDuration("Health", async () => {
        let getRes = await http.asyncRequest('GET', url, {
            headers: {
                Authorization: `Bearer ${config.ESSToken}`,
                "Content-Type": "application/json"
            }
        });

        check(getRes, {
            'is Healthy': (getRes) => getRes.status === 200
        });
    });
    report.manageDuration(group_duration, "health");
}

export function getRequestTypeName(url) {
    if (url.includes('productIdentifiers')) { return 'productIdentifiers'; }
    else if (url.includes('newfilespublished')) { return 'newfilespublished'; }
    else if (url.includes('productVersions')) { return 'productVersions'; }
    else if (url.includes('sinceDateTime')) { return 'sinceDateTime'; }
    else if (url.includes('health')) { return 'health'; }
    else { return url; }
}
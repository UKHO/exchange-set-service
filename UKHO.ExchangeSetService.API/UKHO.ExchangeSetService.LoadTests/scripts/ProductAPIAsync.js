import http from 'k6/http';
import { check, sleep } from 'k6';
import exec from 'k6/execution';

const config = JSON.parse(open('../config.json'));
const report = require('../helper/MetricHelper.js');

var FSSDownloadTime, FSSBatchResponseTime = 0;

/**
 * @param {Object} requestURL The set of Product IDs to create Exchange Set 
 * @param {Object} requestBody The set of Product IDs to create Exchange Set 
 * @param {String} BatchID Batch ID of the created Exchange Set
*/
export async function createExchangeSetAsync(requestURL, requestBody) {
    essRes = await session.asyncPost(requestURL, requestBody,
        { headers: { Authorization: `Bearer ${config.ESSToken}`, "Content-Type": "application/json" } });

    if (check(essRes, {
        'HTTP Response Code': (essRes) => ess.status = 200,
    })) {
        console.log("Batch ID:" + JSON.stringify(essRes['fssBatchId']));
    }
}

/**
 * @param {Object} requestURL The set of Product IDs to create Exchange Set 
 * @param {Object} requestBody The set of Product IDs to create Exchange Set 
 * @param {String} BatchID Batch ID of the created Exchange Set
*/
export function resendRequest(requestURL, requestBody) {
    let essRes = http.post(requestURL, requestBody,
        { headers: { Authorization: `Bearer ${config.ESSToken}`, "Content-Type": "application/json" } });

    if (essRes.status == 401) {
        console.error("Bearer token expired!");
        exec.test.abort("Please update token, Execution Stopped!");
    }

    check(essRes, {
        'HTTP Response Code': (essRes) => essRes.status = 200,
    });

    if (essRes.status == 200) {
        try {
            if (essRes.body != null && typeof essRes.json().fssBatchId === 'undefined') {
                console.log("Response:" + JSON.stringify(essRes.body, null, 2));
                console.log("Correlation-Id:" + essRes.headers['X-Correlation-Id']);
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

export async function CreateESSFromProdIDs(essURL, requestBodyText, reqTypeName) {
    var essResponse;
    var ESSReponseTime;
    essResponse = await http.asyncRequest('POST', essURL, JSON.parse(requestBodyText),
        { headers: { Authorization: `Bearer ${config.ESSToken}`, "Content-Type": "application/json" } });

    check(essResponse, {
        'is ESS Created': (essResponse) => essResponse.status === 200,
    });

    if (essResponse.status == 200 && essResponse.body != '200') {
        let jsonResponse = JSON.parse(essResponse.body);
        var batchStatusUrl = JSON.stringify(jsonResponse['_links']['exchangeSetBatchStatusUri']['href']);
        var batchDetailsUri = JSON.stringify(jsonResponse['_links']['exchangeSetBatchDetailsUri']['href']);

        let fssCommitStatus = GetFSSApiResponse(JSON.parse(batchStatusUrl));
        while (fssCommitStatus !== "Incomplete") {  //"Committed"
            sleep(0.5);
            fssCommitStatus = GetFSSApiResponse(JSON.parse(batchStatusUrl));
            if (fssCommitStatus === "Failed") {
                break;
            }
        };

        check(fssCommitStatus, {
            "is Batch Committed": fssCommitStatus === "Incomplete",//"Committed",
        });
    }
    /*
    ||--------------------------------------------------------------------------------------||
    ||                       Download Exchange Set                                          ||
    ||--------------------------------------------------------------------------------------|| 
    */
    if (config.DownloadFile.DownloadFlag == true) {
        var group_duration = report.GetGroupDuration(reqTypeName + "_download", () => {
            let fssDetailsResponse = GetFSSApiDetailsResponse(JSON.parse(batchDetailsUri));
            let filename = fssDetailsResponse['files'][0]['filename'];
            if (filename.endsWith(".zip") == false) {
                exec.test.abort("Test Failed: ESS Zip creation error");
            }

            let fileSize = JSON.stringify(fssDetailsResponse['files'][0]['fileSize']);
            var fileSizeInMB = (fileSize / 1000000).toFixed(2);
            let url = `${config.FSS_URL}/batch/${fssDetailsResponse['batchId']}/files/${fssDetailsResponse['files'][0]['filename']}`;
            let fssResponse = http.get(url, { headers: { Authorization: `Bearer ${config.FSSToken}`, "Content-Type": "application/json" } });
            FSSDownloadTime = fssResponse.timings.duration;

            if (fileSizeInMB < 50) {
                report.SmallESSMetrics(prodCount, fileSizeInMB, ESSReponseTime, FSSBatchResponseTime, FSSDownloadTime);
            }

            if (fileSizeInMB > 50 && fileSizeInMB < 300) {
                report.MediumESSMetrics(prodCount, fileSizeInMB, ESSReponseTime, FSSBatchResponseTime, FSSDownloadTime);
            }

            if (fileSizeInMB > 300) {
                report.LargeESSMetrics(prodCount, fileSizeInMB, ESSReponseTime, FSSBatchResponseTime, FSSDownloadTime);
            }

            check(fssResponse, { 'is ESS Downloaded': (r) => r.status == 200 });

            report.manageDuration(group_duration, reqTypeName + "_download");
        });
    }
};

export async function GetFSSApiResponse(url) {
    var urlPortion = url.split(config.FSSDetailURI); //Hard coded for environment
    let fssResponse = await http.asyncRequest('GET', `${config.FSS_URL}${urlPortion[1]}`, { headers: { Authorization: `Bearer ${config.FSSToken}`, "Content-Type": "application/json" } });
    FSSBatchResponseTime = FSSBatchResponseTime + fssResponse.timings.duration;
    var batchStatusResponse = JSON.parse(fssResponse.body);
    let fssCommitStatus = JSON.parse(JSON.stringify(batchStatusResponse['status']));
    return fssCommitStatus;
};

export async function GetFSSApiDetailsResponse(url) {
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
        })

        check(getRes, {
            'is Healthy': (getRes) => getRes.status === 200,
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
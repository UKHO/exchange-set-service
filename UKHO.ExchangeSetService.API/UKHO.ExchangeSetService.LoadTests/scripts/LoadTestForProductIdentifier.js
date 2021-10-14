import http from 'k6/http';
import { check, group, sleep } from 'k6';

const apiClient = require('../helper/clientHelper.js');

export function ESSCreation(clientAuthResp, productIdentifierData, exchangeSetType) {
    let fssCommitStatus, batchDetailsUri, batchStatusUrl;

    group('ESS Creation', () => {
        let essResponse = apiClient.GetESSApiResponse("productIdentifiers", productIdentifierData, `${clientAuthResp.essToken}`, exchangeSetType);
        sleep(1);

        let jsonResponse = JSON.parse(essResponse.body);
        batchStatusUrl = JSON.stringify(jsonResponse['_links']['exchangeSetBatchStatusUri']['href']);
        batchDetailsUri = JSON.stringify(jsonResponse['_links']['exchangeSetBatchDetailsUri']['href']);
    
        fssCommitStatus = apiClient.GetFSSApiResponse(JSON.parse(batchStatusUrl), `${clientAuthResp.fssToken}`);

        while (fssCommitStatus !== "Committed") {
            sleep(10);
            fssCommitStatus = apiClient.GetFSSApiResponse(JSON.parse(batchStatusUrl), `${clientAuthResp.fssToken}`);

            if (fssCommitStatus === "Failed") {
                break;
            }
        };
    });

    check(fssCommitStatus, {
        "status is Committed": fssCommitStatus === "Committed",
    })

    let batchId = GetBatchId(batchDetailsUri);
    console.log("batchId : " + batchId + " fssCommitStatus : ", fssCommitStatus, exchangeSetType);
}

export function GetBatchId(str) {
    let revBatchDetailsUri = str.split('').reverse().join('');
    let revBatchId = revBatchDetailsUri.substring(1, revBatchDetailsUri.indexOf("/"))
    let batchId = revBatchId.split('').reverse().join('');
    return batchId;
}
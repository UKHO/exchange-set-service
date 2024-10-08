import http from 'k6/http';
import { check, group, sleep } from 'k6';

const apiClient = require('../Helper/ClientHelper.js');
export function ESSCreation(clientAuthResp, productIdentifierData, exchangeSetType) {
    let fssCommitStatus, batchDetailsUri, batchStatusUrl, fssDetailsResponse, filename;
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
        fssDetailsResponse = apiClient.GetFSSApiDetailsResponse(JSON.parse(batchDetailsUri), `${clientAuthResp.fssToken}`);

    });

    filename = fssDetailsResponse['files'][0]['filename'];

    check(fssCommitStatus, {
        "status is Committed": fssCommitStatus === "Committed",
    });

    check(filename, {
         "file is Created": (filename === "V01X01.zip" || filename === "M01X01.zip" || filename === "M02X02.zip"),
    });

    let batchId = GetBatchId(batchDetailsUri);
    
    console.log("batchId : " + batchId + " fssCommitStatus : ", fssCommitStatus, exchangeSetType);
    console.log("batchId : " + batchId + " Filename : " + filename);

    return fssDetailsResponse;
}

export function GetBatchId(str) {
    let revBatchDetailsUri =str.split('').reverse().join('');
    let revBatchId =revBatchDetailsUri.substring(1, revBatchDetailsUri.indexOf("/"))
    let batchId = revBatchId.split('').reverse().join('');
    return batchId;
}
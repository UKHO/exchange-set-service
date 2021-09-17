import http from 'k6/http';
import { check, group, sleep } from 'k6';

const apiClient = require('./clientHelper.js');

export function ESSCreation(clientAuthResp, productIdentifierData, exchangeSetType) {
    let fssCommitStatus;
    group('ESS Creation', () => {
        let batchStatusUrl = apiClient.GetESSApiResponse("productIdentifiers", productIdentifierData, `${clientAuthResp.essToken}`, exchangeSetType);
        sleep(1);
        console.log("batchStatusUrl", batchStatusUrl);
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
}
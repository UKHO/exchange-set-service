import http from 'k6/http';
import { check, group } from 'k6';
import { Trend } from 'k6/metrics';

const config = JSON.parse(open('./config.json'));
let ESSApiTrend = new Trend('EssApi_time');

export function GetESSApiResponse(endPoint, data, essToken) {
    let essResponse;
    var essUrl = `${config.Base_URL}/productData/${endPoint}`;
    group('ESS Api Response', () => {
        essResponse = http.post(essUrl, JSON.stringify(data), { headers: { Authorization: `Bearer ${essToken}`, "Content-Type": "application/json" } });
    });
    console.log("essResponse", JSON.stringify(essResponse))
    check(essResponse, {
        'is ESS status 200': (essResponse) => essResponse.status === 200,
    });

    ESSApiTrend.add(essResponse.timings.waiting);
    console.log("ESSApiTrend", ESSApiTrend.name);

    let jsonResponse = JSON.parse(essResponse.body);
    let batchStatusUrl = JSON.stringify(jsonResponse['_links']['exchangeSetBatchStatusUri']['href']);

    return batchStatusUrl;
};

export function GetFSSApiResponse(url, fssToken) {
    let fssResponse = http.get(url, { headers: { Authorization: `Bearer ${fssToken}`, "Content-Type": "application/json" } });

    var batchStatusResponse = JSON.parse(fssResponse.body)
    let fssCommitStatus = JSON.parse(JSON.stringify(batchStatusResponse['status']));

    return fssCommitStatus;
};
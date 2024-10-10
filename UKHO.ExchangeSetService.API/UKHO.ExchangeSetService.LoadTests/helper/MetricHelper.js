import { Gauge, Trend, Counter } from 'k6/metrics';
import { group } from 'k6';

const s63SmallEssCreationTime = new Trend('SmallEssCreationTime');
const s63SmallEssBatchTime = new Trend('SmallEssBatchTime');
const s63SmallEssDownloadTime = new Trend('SmallEssDownloadTime');
const s63SmallEssProductCount = new Gauge('SmallEssProductCount');
const s63SmallEssSizeRange = new Gauge('SmallEssSizeRange');
const s63SmallEssE2E = new Trend('SmallEssE2ETime_Minutes');

const s63MediumEssCreationTime = new Trend('MediumEssCreationTime');
const s63MediumEssBatchTime = new Trend('MediumEssBatchTime');
const s63MediumEssDownloadTime = new Trend('MediumEssDownloadTime');
const s63MediumEssProductCount = new Gauge('MediumEssProductCount');
const s63MediumEssSizeRange = new Gauge('MediumEssSizeRange');
const s63MediumEssE2E = new Trend('MediumEssE2ETime_Minutes');

const s63LargeEssCreationTime = new Trend('LargeEssCreationTime');
const s63LargeEssBatchTime = new Trend('LargeEssBatchTime');
const s63LargeEssDownloadTime = new Trend('LargeEssDownloadTime');
const s63LargeEssProductCount = new Gauge('LargeESSProductsCount');
const s63LargeEssSizeRange = new Gauge('LargeEssSizeRange');
const s63LargeEssE2E = new Trend('LargeEssE2ETime_Minutes');

const productIdentifiersTime = new Trend('ESCreation_PI');
const productVersionsTime = new Trend('ESCreation_PV');
const sinceDateTime = new Trend('ESCreation_SD');
const newfilespublishedTime = new Trend('ESCreation_Publish');
const healthTime = new Trend('Health');

const productIdentifiersCounter = new Counter('Counts_PI');
const productVersionsCounter = new Counter('Counts_PV');
const sinceDateTimeCounter = new Counter('Counts_SD');
const newfilespublishedCounter = new Counter('Counts_Publish');
const healthCounter = new Counter('Counts_Health');

export function smallESSMetrics(prodCount, fileSizeInMB, ESSReponseTime, FSSBatchResponseTime) {
        s63SmallEssProductCount.add(prodCount);
        s63SmallEssSizeRange.add(fileSizeInMB);
        s63SmallEssCreationTime.add(ESSReponseTime, true);
        s63SmallEssBatchTime.add(FSSBatchResponseTime, true);
        s63SmallEssDownloadTime.add(FSSBatchResponseTime, true);
        s63SmallEssE2E.add(((ESSReponseTime + FSSBatchResponseTime + FSSBatchResponseTime) / 60000).toFixed(2), true);
}

export function mediumESSMetrics(prodCount, fileSizeInMB, ESSReponseTime, FSSBatchResponseTime) {
        s63MediumEssProductCount.add(prodCount);
        s63MediumEssSizeRange.add(fileSizeInMB);
        s63MediumEssCreationTime.add(ESSReponseTime, true);
        s63MediumEssBatchTime.add(FSSBatchResponseTime, true);
        s63MediumEssDownloadTime.add(FSSBatchResponseTime, true);
        s63MediumEssE2E.add(((ESSReponseTime + FSSBatchResponseTime + FSSBatchResponseTime) / 60000).toFixed(2), true);
}

export function largeESSMetrics(prodCount, fileSizeInMB, ESSReponseTime, FSSBatchResponseTime) {
        s63LargeEssProductCount.add(prodCount);
        s63LargeEssSizeRange.add(fileSizeInMB);
        s63LargeEssCreationTime.add(ESSReponseTime, true);
        s63LargeEssBatchTime.add(FSSBatchResponseTime, true);
        s63LargeEssDownloadTime.add(FSSBatchResponseTime, true);
        s63LargeEssE2E.add(((ESSReponseTime + FSSBatchResponseTime + FSSBatchResponseTime) / 60000).toFixed(2), true);
}

export function manageDuration(duration, method) {
        switch (method) {
                case 'productIdentifiers':
                        productIdentifiersTime.add(duration);
                        productIdentifiersCounter.add(1);
                        break;

                case 'productVersions':
                        productVersionsTime.add(duration);
                        productVersionsCounter.add(1);
                        break;

                case 'sinceDateTime':
                        sinceDateTime.add(duration);
                        sinceDateTimeCounter.add(1);
                        break;

                case 'newfilespublished':
                        newfilespublishedTime.add(duration);
                        newfilespublishedCounter.add(1);
                        break;

                case 'health':
                        healthTime.add(duration);
                        healthCounter.add(1);
                        break;

                default:
                        console.error("Unhandled request type for report" + method);
        }
}

export function getGroupDuration(groupName, f) {
        var start = new Date();
        group(groupName, f);
        return new Date() - start;
}

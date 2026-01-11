import { PersistedState } from "runed";

export interface WizardStateData {
  setupType: 'fresh' | 'migrate' | 'compatibility-proxy';
  migration?: {
    mode: 'Api' | 'MongoDb';
    nightscoutUrl: string;
    nightscoutApiSecret: string;
    mongoConnectionString?: string;
    mongoDatabaseName?: string;
  };
  compatibilityProxy?: {

    nightscoutUrl: string;
    nightscoutApiSecret: string;
    enableDetailedLogging?: boolean;
  };
  postgres: {
    useContainer: boolean;
    connectionString?: string;
  };
  optionalServices: {
    watchtower: boolean;
    includeDashboard: boolean;
    includeScalar: boolean;
  };
  selectedConnectors: string[];
  connectorConfigs: Record<string, Record<string, string>>;
}

class WizardStore {
  setupType = $state<'fresh' | 'migrate' | 'compatibility-proxy'>('fresh');
  migration = $state<WizardStateData['migration']>(undefined);
  compatibilityProxy = $state<WizardStateData['compatibilityProxy']>(undefined);
  postgres = $state<WizardStateData['postgres']>({ useContainer: true });
  optionalServices = $state<WizardStateData['optionalServices']>({
    watchtower: true,
    includeDashboard: true,
    includeScalar: true
  });
  selectedConnectors = $state<string[]>([]);
  connectorConfigs = new PersistedState<Record<string, Record<string, string>>>('wizard-connector-configs', {});

  setSetupType(type: WizardStateData['setupType']) {
    this.setupType = type;
  }

  setMigration(config: WizardStateData['migration']) {
    this.migration = config;
  }

  setCompatibilityProxy(config: WizardStateData['compatibilityProxy']) {
    this.compatibilityProxy = config;
  }

  setPostgres(config: WizardStateData['postgres']) {
    this.postgres = config;
  }

  setOptionalServices(config: WizardStateData['optionalServices']) {
    this.optionalServices = config;
  }

  toggleConnector(connectorType: string) {
    if (this.selectedConnectors.includes(connectorType)) {
      this.selectedConnectors = this.selectedConnectors.filter(c => c !== connectorType);
      const { [connectorType]: _, ...rest } = this.connectorConfigs.current;
      this.connectorConfigs.current = rest;
    } else {
      this.selectedConnectors = [...this.selectedConnectors, connectorType];
      this.connectorConfigs.current = { ...this.connectorConfigs.current, [connectorType]: {} };
    }
  }

  setConnectorConfig(connectorType: string, config: Record<string, string>) {
    this.connectorConfigs.current = { ...this.connectorConfigs.current, [connectorType]: config };
  }

  reset() {
    this.setupType = 'fresh';
    this.migration = undefined;
    this.compatibilityProxy = undefined;
    this.postgres = { useContainer: true };
    this.optionalServices = { watchtower: true, includeDashboard: true, includeScalar: true };
    this.selectedConnectors = [];
    this.connectorConfigs.current = {};
  }
}

export const wizardStore = new WizardStore();

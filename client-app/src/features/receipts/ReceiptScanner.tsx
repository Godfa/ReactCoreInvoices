import React, { useState } from 'react';
import { Button, Form, Segment, Header, Message, Table, Loader, Dropdown, Label } from 'semantic-ui-react';
import agent, { ScannedReceiptResult } from '../../app/api/agent';
import { toast } from 'react-toastify';

interface ReceiptScannerProps {
  onScanComplete?: (result: ScannedReceiptResult) => void;
}

const ReceiptScanner: React.FC<ReceiptScannerProps> = ({ onScanComplete }) => {
  const [file, setFile] = useState<File | null>(null);
  const [loading, setLoading] = useState(false);
  const [provider, setProvider] = useState<'auto' | 'azure' | 'ollama'>('auto');
  const [ollamaModel, setOllamaModel] = useState('llama3.2-vision');
  const [language, setLanguage] = useState('fi');
  const [result, setResult] = useState<ScannedReceiptResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [showAdvanced, setShowAdvanced] = useState(false);

  const providerOptions = [
    { key: 'auto', text: '🤖 Automaattinen (Azure → Ollama)', value: 'auto' },
    { key: 'azure', text: '☁️ Azure AI (pilvi, nopea)', value: 'azure' },
    { key: 'ollama', text: '💻 Ollama (paikallinen, ilmainen)', value: 'ollama' }
  ];

  const ollamaModelOptions = [
    { key: 'llama3.2-vision', text: 'llama3.2-vision (suositeltu)', value: 'llama3.2-vision' },
    { key: 'llava', text: 'llava (nopea, pienempi)', value: 'llava' },
    { key: 'llava:13b', text: 'llava:13b (tarkempi)', value: 'llava:13b' },
    { key: 'llava:34b', text: 'llava:34b (paras laatu)', value: 'llava:34b' },
    { key: 'bakllava', text: 'bakllava (vaihtoehto)', value: 'bakllava' }
  ];

  const languageOptions = [
    { key: 'fi', text: 'Suomi', value: 'fi' },
    { key: 'en', text: 'English', value: 'en' },
    { key: 'sv', text: 'Svenska', value: 'sv' }
  ];

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      const selectedFile = e.target.files[0];

      // Validate file type
      const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp'];
      if (!allowedTypes.includes(selectedFile.type)) {
        toast.error('Virheellinen tiedostotyyppi. Vain JPEG, PNG ja WEBP kuvat ovat tuettuja.');
        return;
      }

      // Validate file size (10MB max)
      if (selectedFile.size > 10_000_000) {
        toast.error('Tiedosto on liian suuri. Maksimikoko on 10MB.');
        return;
      }

      setFile(selectedFile);
      setResult(null);
      setError(null);
    }
  };

  const scanReceipt = async () => {
    if (!file) return;

    setLoading(true);
    setError(null);

    try {
      const scanResult = await agent.Receipts.scan(
        file,
        language,
        provider,
        provider === 'ollama' ? ollamaModel : undefined
      );

      setResult(scanResult);
      toast.success('Kuitti skannattu onnistuneesti!');

      if (onScanComplete) {
        onScanComplete(scanResult);
      }
    } catch (err: any) {
      const message = err.response?.data?.error || err.response?.data?.message || err.message || 'Skannaus epäonnistui';
      setError(message);
      toast.error(`Virhe: ${message}`);
      console.error('Receipt scan error:', err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Segment>
      <Header as="h2">📄 Skannaa kuitti</Header>

      <Form>
        <Form.Field>
          <label>Valitse kuittikuva</label>
          <input
            type="file"
            accept="image/jpeg,image/jpg,image/png,image/webp"
            onChange={handleFileChange}
            disabled={loading}
          />
          {file && (
            <Label color="blue" style={{ marginTop: '10px' }}>
              {file.name} ({(file.size / 1024).toFixed(2)} KB)
            </Label>
          )}
        </Form.Field>

        <Form.Field>
          <Button
            type="button"
            basic
            size="small"
            onClick={() => setShowAdvanced(!showAdvanced)}
          >
            {showAdvanced ? '▼' : '▶'} Lisäasetukset
          </Button>
        </Form.Field>

        {showAdvanced && (
          <Segment>
            <Form.Field>
              <label>Skannauspalvelu</label>
              <Dropdown
                selection
                options={providerOptions}
                value={provider}
                onChange={(_, data) => setProvider(data.value as any)}
                disabled={loading}
              />
            </Form.Field>

            {provider === 'ollama' && (
              <Form.Field>
                <label>Ollama-malli</label>
                <Dropdown
                  selection
                  options={ollamaModelOptions}
                  value={ollamaModel}
                  onChange={(_, data) => setOllamaModel(data.value as string)}
                  disabled={loading}
                />
                <small>Vaihtoehto muuttaa tarkkuutta ja nopeutta</small>
              </Form.Field>
            )}

            <Form.Field>
              <label>Kieli</label>
              <Dropdown
                selection
                options={languageOptions}
                value={language}
                onChange={(_, data) => setLanguage(data.value as string)}
                disabled={loading}
              />
            </Form.Field>
          </Segment>
        )}

        <Button
          primary
          onClick={scanReceipt}
          disabled={!file || loading}
        >
          {loading ? 'Skannataan...' : 'Skannaa kuitti'}
        </Button>
      </Form>

      {loading && (
        <Segment textAlign="center">
          <Loader active inline="centered">
            Skannataan kuittiä...
          </Loader>
        </Segment>
      )}

      {error && (
        <Message negative>
          <Message.Header>Virhe</Message.Header>
          <p>{error}</p>
        </Message>
      )}

      {result && !loading && (
        <Segment>
          <Header as="h3">✅ Skannaustulos</Header>
          <p>
            <strong>Palvelu:</strong> {result.provider} |
            <strong> Aika:</strong> {result.processingTimeMs}ms
          </p>

          {result.merchantName && (
            <p><strong>Kauppias:</strong> {result.merchantName}</p>
          )}
          {result.receiptDate && (
            <p><strong>Päivämäärä:</strong> {new Date(result.receiptDate).toLocaleDateString('fi-FI')}</p>
          )}

          {result.lines.length > 0 && (
            <Table celled>
              <Table.Header>
                <Table.Row>
                  <Table.HeaderCell>Tuote</Table.HeaderCell>
                  <Table.HeaderCell>Määrä</Table.HeaderCell>
                  <Table.HeaderCell>Hinta</Table.HeaderCell>
                  <Table.HeaderCell>Yhteensä</Table.HeaderCell>
                </Table.Row>
              </Table.Header>
              <Table.Body>
                {result.lines.map((line, idx) => (
                  <Table.Row key={idx}>
                    <Table.Cell>{line.description}</Table.Cell>
                    <Table.Cell>{line.quantity}</Table.Cell>
                    <Table.Cell>{line.unitPrice.toFixed(2)} €</Table.Cell>
                    <Table.Cell><strong>{line.lineTotal.toFixed(2)} €</strong></Table.Cell>
                  </Table.Row>
                ))}
              </Table.Body>
            </Table>
          )}

          {result.total && (
            <Segment color="green">
              <Header as="h3" textAlign="center">
                Loppusumma: {result.total.toFixed(2)} €
              </Header>
            </Segment>
          )}

          {result.warnings.length > 0 && (
            <Message warning>
              <Message.Header>Huomautukset</Message.Header>
              <Message.List>
                {result.warnings.map((warning, idx) => (
                  <Message.Item key={idx}>{warning}</Message.Item>
                ))}
              </Message.List>
            </Message>
          )}
        </Segment>
      )}
    </Segment>
  );
};

export default ReceiptScanner;

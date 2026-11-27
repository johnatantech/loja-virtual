import { comBaseDaApi } from './api-url';

describe('Endereço da API', () => {
  it('em desenvolvimento mantém o caminho relativo (o proxy do ng serve resolve)', () => {
    expect(comBaseDaApi('/api/produtos', '')).toBe('/api/produtos');
  });

  it('na publicação prefixa só as chamadas da API', () => {
    const base = 'https://loja-api.azurewebsites.net';
    expect(comBaseDaApi('/api/produtos?pagina=1', base)).toBe(`${base}/api/produtos?pagina=1`);
    expect(comBaseDaApi('https://viacep.com.br/ws/01310100/json/', base)).toBe('https://viacep.com.br/ws/01310100/json/');
  });
});

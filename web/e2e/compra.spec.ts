import { expect, test } from '@playwright/test';

/**
 * Fluxo principal da loja, do cadastro à entrega, como um cliente e um administrador fariam.
 * Os testes rodam em sequência porque o segundo usa o pedido criado pelo primeiro.
 */
test.describe.serial('Compra completa', () => {
  const email = `e2e-${Date.now()}@exemplo.com`;
  let urlPedido = '';

  test.beforeEach(async ({ page }) => {
    // O ViaCEP é um serviço externo: simulado aqui para o teste não depender da internet.
    await page.route('https://viacep.com.br/**', (rota) =>
      rota.fulfill({ json: { logradouro: 'Avenida Paulista', bairro: 'Bela Vista', localidade: 'São Paulo', uf: 'SP' } }),
    );
  });

  test('cliente se cadastra, compra e paga', async ({ page }) => {
    await page.goto('/cadastro');
    await page.locator('input[formcontrolname="nome"]').fill('Cliente E2E');
    await page.locator('input[formcontrolname="email"]').fill(email);
    await page.locator('input[formcontrolname="senha"]').fill('SenhaE2e123');
    await page.getByRole('button', { name: 'Criar conta' }).click();
    await expect(page.getByRole('link', { name: 'Olá, Cliente' })).toBeVisible();

    await page.goto('/produto/codigo-limpo');
    await page.getByRole('button', { name: 'Adicionar ao carrinho' }).click();
    await expect(page.getByText('Adicionado ao carrinho.')).toBeVisible();

    await page.goto('/carrinho');
    await page.getByRole('link', { name: 'Finalizar compra' }).click();

    await page.locator('input[formcontrolname="cep"]').fill('01310100');
    await expect(page.locator('input[formcontrolname="logradouro"]')).toHaveValue('Avenida Paulista');
    await expect(page.getByText(/Entrega em até \d+ dias úteis/)).toBeVisible();
    await page.locator('input[formcontrolname="numero"]').fill('100');
    await page.getByRole('button', { name: 'Ir para o pagamento' }).click();

    await expect(page).toHaveURL(/\/pagamento-simulado\//);
    await page.getByRole('button', { name: 'Aprovar pagamento' }).click();

    await expect(page).toHaveURL(/\/pedidos\/[0-9a-f-]+$/);
    await expect(page.getByText('Pagamento confirmado')).toBeVisible();
    await expect(page.locator('.status.grande')).toHaveText('Pago');
    urlPedido = page.url();
  });

  test('administrador envia e entrega o pedido', async ({ page }) => {
    const emailAdmin = process.env['E2E_ADMIN_EMAIL'];
    const senhaAdmin = process.env['E2E_ADMIN_SENHA'];
    test.skip(!emailAdmin || !senhaAdmin, 'Defina E2E_ADMIN_EMAIL e E2E_ADMIN_SENHA para testar o painel.');

    await page.goto('/entrar');
    await page.locator('input[formcontrolname="email"]').fill(emailAdmin!);
    await page.locator('input[formcontrolname="senha"]').fill(senhaAdmin!);
    await page.getByRole('button', { name: 'Entrar', exact: true }).click();
    await expect(page.getByRole('link', { name: 'Painel' })).toBeVisible();

    const pedidoId = urlPedido.split('/').pop()!;
    await page.goto(`/admin/pedidos/${pedidoId}`);
    await page.getByRole('button', { name: 'Marcar como enviado' }).click();
    await expect(page.getByRole('button', { name: 'Confirmar entrega' })).toBeVisible();
    await page.getByRole('button', { name: 'Confirmar entrega' }).click();
    await expect(page.locator('.barra .status')).toHaveText('Entregue');
  });
});

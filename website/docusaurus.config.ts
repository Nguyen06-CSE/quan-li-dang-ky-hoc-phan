import { themes as prismThemes } from 'prism-react-renderer';
import type { Config } from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'Hệ Thống Quản Lý Đăng Ký Học Phần',
  tagline: 'Tài Liệu Kỹ Thuật & Báo Cáo Đồ Án Ứng Dụng Desktop (.NET 8 / Avalonia UI)',
  favicon: 'img/favicon.ico',

  url: 'https://Nguyen06-CSE.github.io',
  baseUrl: '/quan-li-dang-ky-hoc-phan/',

  organizationName: 'Nguyen06-CSE',
  projectName: 'quan-li-dang-ky-hoc-phan',
  trailingSlash: false,

  onBrokenLinks: 'warn',

  markdown: {
    mermaid: true,
  },
  themes: ['@docusaurus/theme-mermaid'],

  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
          path: '../docs',
          routeBasePath: '/',
          editUrl: 'https://github.com/Nguyen06-CSE/quan-li-dang-ky-hoc-phan/tree/main/',
        },
        blog: false,
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    colorMode: {
      defaultMode: 'light',
      disableSwitch: false,
      respectPrefersColorScheme: true,
    },
    navbar: {
      title: 'QuanLyDKHP Docs',
      logo: {
        alt: 'Logo',
        src: 'img/logo.svg',
        srcDark: 'img/logo.svg',
        width: 32,
        height: 32,
      },
      items: [
        {
          type: 'doc',
          docId: 'index',
          position: 'left',
          label: 'Tổng Quan',
        },
        {
          type: 'doc',
          docId: 'dac-ta-ung-dung/tong-quan-yeu-cau',
          position: 'left',
          label: 'Đặc Tả Ứng Dụng',
        },
        {
          type: 'doc',
          docId: 'ket-qua-dat-duoc/tong-ket-chuc-nang',
          position: 'left',
          label: 'Kết Quả Đạt Được',
        },
        {
          type: 'doc',
          docId: 'nhat-ky-va-minh-chung/lich-su-commit-github',
          position: 'left',
          label: 'Nhật Ký & Minh Chứng',
        },
        {
          href: 'https://github.com/Nguyen06-CSE/quan-li-dang-ky-hoc-phan',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'light',
      links: [
        {
          title: 'Đặc Tả Ứng Dụng',
          items: [
            {
              label: 'Tổng Quan Yêu Cầu',
              to: '/dac-ta-ung-dung/tong-quan-yeu-cau',
            },
            {
              label: 'Kiến Trúc Hệ Thống',
              to: '/dac-ta-ung-dung/kien-truc-he-thong',
            },
            {
              label: 'Thiết Kế CSDL & ERD',
              to: '/dac-ta-ung-dung/co-so-du-lieu-erd',
            },
            {
              label: 'Ma Trận Phân Quyền',
              to: '/dac-ta-ung-dung/ma-tran-phan-quyen',
            },
          ],
        },
        {
          title: 'Kết Quả & Sản Phẩm',
          items: [
            {
              label: 'Ma Trận Đối Chiếu',
              to: '/ket-qua-dat-duoc/tong-ket-chuc-nang',
            },
            {
              label: 'Báo Cáo Unit Test',
              to: '/ket-qua-dat-duoc/kiem-thu-unit-test',
            },
            {
              label: 'Tối Ưu Hiệu Năng',
              to: '/ket-qua-dat-duoc/toi-uu-hieu-nang',
            },
            {
              label: 'Hướng Dẫn Cài Đặt',
              to: '/ket-qua-dat-duoc/huong-dan-cai-dat-khoi-chay',
            },
          ],
        },
        {
          title: 'Minh Chứng Quá Trình',
          items: [
            {
              label: 'Lịch Sử Git Commits',
              to: '/nhat-ky-va-minh-chung/lich-su-commit-github',
            },
            {
              label: 'Nhật Ký Công Việc Daily',
              to: '/nhat-ky-va-minh-chung/nhat-ky-cong-viec-daily',
            },
            {
              label: 'Nhật Ký Quyết Định ADR',
              to: '/nhat-ky-va-minh-chung/nhat-ky-quyet-dinh-ky-thuat',
            },
            {
              label: 'Nhật Ký Prompts Agent',
              to: '/nhat-ky-va-minh-chung/nhat-ky-prompts-agent',
            },
          ],
        },
      ],
      copyright: `Báo cáo Đồ án Môn học Xây dựng Ứng dụng Desktop - Khoa Công nghệ Thông tin, Đại học Đà Lạt.<br/>Bản quyền © ${new Date().getFullYear()} Nhóm phát triển QuanLyDKHP.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ['csharp', 'json', 'bash', 'sql', 'powershell'],
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
